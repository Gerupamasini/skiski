using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Skiski.EditorTools
{
    /// <summary>
    /// 初回オープン時に URP の初期設定とサンプルシーンの作成を行うエディタ拡張。
    ///
    /// - Default Render Pipeline が未設定なら、起動時に一度だけセットアップを提案する
    /// - メニュー「Tools/Skiski」からいつでも再実行できる（既存アセットは上書きしない）
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string MenuRoot = "Tools/Skiski/";

        const string SettingsFolder = "Assets/Settings";
        const string PipelineAssetPath = SettingsFolder + "/URP-Default.asset";
        const string RendererDataPath = SettingsFolder + "/URP-Default_Renderer.asset";
        const string VolumeProfilePath = SettingsFolder + "/SampleScene_VolumeProfile.asset";
        const string MaterialsFolder = "Assets/Materials";
        const string ScenesFolder = "Assets/Scenes";
        const string SampleScenePath = ScenesFolder + "/SampleScene.unity";

        const string DefaultPostProcessDataPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        const string LitShaderName = "Universal Render Pipeline/Lit";

        const string AskedThisSessionKey = "Skiski.ProjectSetup.AskedThisSession";
        const string DialogTitle = "Skiski プロジェクトセットアップ";

        static ProjectSetup()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.update += WaitUntilEditorIsReady;
        }

        static void WaitUntilEditorIsReady()
        {
            // インポートやコンパイルが終わるまで待つ
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= WaitUntilEditorIsReady;

            // すでに何らかのレンダーパイプラインが設定済みなら何もしない
            if (GraphicsSettings.defaultRenderPipeline != null)
                return;

            // 「あとで」を選んだ場合、同じエディタセッション中は再度聞かない
            if (SessionState.GetBool(AskedThisSessionKey, false))
                return;
            SessionState.SetBool(AskedThisSessionKey, true);

            bool run = EditorUtility.DisplayDialog(
                DialogTitle,
                "URP（Universal Render Pipeline）がまだ設定されていません。\n\n" +
                "次の初期設定を行いますか？\n" +
                "・URP アセット / Renderer を作成して Graphics 設定に割り当て\n" +
                "・カラースペースを Linear に設定\n" +
                "・動作確認用のサンプルシーンを作成\n\n" +
                "あとで実行する場合はメニュー「Tools > Skiski > Setup Project (URP)」から。",
                "セットアップする",
                "あとで");

            if (run)
                SetupProject();
        }

        [MenuItem(MenuRoot + "Setup Project (URP)", priority = 0)]
        public static void SetupProject()
        {
            UniversalRenderPipelineAsset pipeline = SetupUniversalRenderPipeline();
            SetupPlayerSettings();
            bool sceneReady = CreateSampleScene(overwrite: false);

            AssetDatabase.SaveAssets();

            Debug.Log($"[Skiski] セットアップ完了: Render Pipeline = {AssetDatabase.GetAssetPath(pipeline)}");
            EditorUtility.DisplayDialog(
                DialogTitle,
                "セットアップが完了しました。\n\n" +
                (sceneReady
                    ? $"{SampleScenePath} を開いた状態で Play ボタンを押すと動作確認できます。\n" +
                      "（WASD / 矢印キー / 左スティックで移動、Space でジャンプ）\n\n"
                    : "") +
                "生成されたファイル（Assets/, ProjectSettings/, Packages/packages-lock.json）は Git にコミットしてください。",
                "OK");
        }

        [MenuItem(MenuRoot + "Recreate Sample Scene", priority = 20)]
        static void RecreateSampleScene()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                EditorUtility.DisplayDialog(DialogTitle,
                    "先に「Tools > Skiski > Setup Project (URP)」を実行してください。", "OK");
                return;
            }

            if (File.Exists(SampleScenePath) &&
                !EditorUtility.DisplayDialog(DialogTitle,
                    $"{SampleScenePath} を作り直します（変更内容は失われます）。よろしいですか？",
                    "作り直す", "キャンセル"))
                return;

            CreateSampleScene(overwrite: true);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // URP
        // ------------------------------------------------------------------

        static UniversalRenderPipelineAsset SetupUniversalRenderPipeline()
        {
            EnsureFolder(SettingsFolder);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    // メニュー「Create > Rendering > URP Asset (with Universal Renderer)」と同じく
                    // パッケージ同梱の PostProcessData を割り当てる（ポストエフェクトに必要）
                    rendererData.postProcessData =
                        AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
                    if (rendererData.postProcessData == null)
                        Debug.LogWarning($"[Skiski] {DefaultPostProcessDataPath} が見つかりません。" +
                                         "Renderer の Post-processing Data を手動で設定してください。");

                    AssetDatabase.CreateAsset(rendererData, RendererDataPath);
                }

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            // Project Settings > Graphics > Default Render Pipeline
            // （Quality レベル側が未設定ならこの値が使われる）
            GraphicsSettings.defaultRenderPipeline = pipeline;
            return pipeline;
        }

        static void SetupPlayerSettings()
        {
            // URP の PBR ライティングは Linear 前提
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        // ------------------------------------------------------------------
        // サンプルシーン
        // ------------------------------------------------------------------

        /// <returns>サンプルシーンが開かれた状態になったら true</returns>
        static bool CreateSampleScene(bool overwrite)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            if (!overwrite && File.Exists(SampleScenePath))
            {
                EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
                AddSceneToBuildSettings(SampleScenePath);
                return true;
            }

            EnsureFolder(ScenesFolder);
            EnsureFolder(MaterialsFolder);

            // Main Camera と Directional Light を含む新規シーン
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Material groundMaterial = GetOrCreateLitMaterial("Ground", new Color(0.55f, 0.6f, 0.65f));
            Material blockMaterial = GetOrCreateLitMaterial("Block", new Color(0.25f, 0.5f, 0.9f));
            Material playerMaterial = GetOrCreateLitMaterial("Player", new Color(1f, 0.45f, 0.2f));

            // 地面（Plane は 10m 四方なので 50m 四方にする）
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            // 位置関係の目安になるブロック
            var blocks = new GameObject("Blocks");
            Vector3[] blockPositions =
            {
                new Vector3(4f, 0.5f, 4f),
                new Vector3(-5f, 0.5f, 2f),
                new Vector3(2f, 0.5f, -6f),
                new Vector3(-3f, 1f, -4f),
            };
            for (int i = 0; i < blockPositions.Length; i++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Block_{i + 1}";
                block.transform.SetParent(blocks.transform, false);
                block.transform.position = blockPositions[i];
                block.transform.localScale = Vector3.one * (blockPositions[i].y * 2f);
                block.GetComponent<Renderer>().sharedMaterial = blockMaterial;
            }

            // プレイヤー（CharacterController が独自のカプセル判定を持つので CapsuleCollider は外す）
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = Vector3.zero;
            player.AddComponent<PlayerController>();

            // カメラとライト
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out Camera camera))
                {
                    var follow = camera.gameObject.AddComponent<CameraFollow>();
                    follow.Target = player.transform;
                    camera.transform.position = player.transform.position + follow.Offset;
                    camera.transform.LookAt(player.transform.position + Vector3.up);

                    // URP のカメラ設定: ポストエフェクトを有効化
                    camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                }
                else if (root.TryGetComponent(out Light light) && light.type == UnityEngine.LightType.Directional)
                {
                    light.shadows = LightShadows.Soft;
                }
            }

            // ポストエフェクト用のグローバル Volume
            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetOrCreateVolumeProfile();

            EditorSceneManager.SaveScene(scene, SampleScenePath);
            AddSceneToBuildSettings(SampleScenePath);
            return true;
        }

        static VolumeProfile GetOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
                return profile;

            profile = UnityEditor.Rendering.VolumeProfileFactory.CreateVolumeProfileAtPath(VolumeProfilePath);

            var tonemapping = UnityEditor.Rendering.VolumeProfileFactory
                .CreateVolumeComponent<Tonemapping>(profile, overrides: false, saveAsset: false);
            tonemapping.mode.Override(TonemappingMode.ACES);
            EditorUtility.SetDirty(tonemapping);

            var bloom = UnityEditor.Rendering.VolumeProfileFactory
                .CreateVolumeComponent<Bloom>(profile, overrides: false, saveAsset: false);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.5f);
            EditorUtility.SetDirty(bloom);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static Material GetOrCreateLitMaterial(string name, Color baseColor)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            Shader lit = Shader.Find(LitShaderName);
            if (lit != null)
            {
                material = new Material(lit);
            }
            else
            {
                // 念のため: 現在のパイプラインの既定マテリアルを複製
                Debug.LogWarning($"[Skiski] シェーダー \"{LitShaderName}\" が見つからないため既定マテリアルを使用します。");
                material = new Material(GraphicsSettings.defaultRenderPipeline.defaultMaterial);
            }

            material.name = name;
            material.SetColor("_BaseColor", baseColor);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------
        // ユーティリティ
        // ------------------------------------------------------------------

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
