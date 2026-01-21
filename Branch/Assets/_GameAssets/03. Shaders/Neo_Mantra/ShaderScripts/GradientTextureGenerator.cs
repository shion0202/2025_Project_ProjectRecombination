using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class GradientTextureGenerator : MonoBehaviour
{
    public Gradient gradient;
    public int width = 128, height = 1;
    public string textureName = "GeneratedGradient";
    public string savePath = "Assets/_ProjectAssets/03. Textures/Gradient";     // 저장 경로
    public Texture2D generatedTexture;

#if UNITY_EDITOR
    public void BakeGradient()
    {
        string newSavePath = savePath;
        if (generatedTexture)
        {
            newSavePath = Path.Combine(newSavePath, generatedTexture.name + ".png");
        }
        else
        {
            newSavePath = Path.Combine(newSavePath, textureName + ".png");
        }

        // 텍스처 생성
        if (generatedTexture == null || generatedTexture.width != width || generatedTexture.height != height)
        {
            generatedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            generatedTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // 픽셀 채우기
        for (int x = 0; x < width; x++)
        {
            Color c = gradient.Evaluate(x / (float)(width - 1));
            for (int y = 0; y < height; y++)
                generatedTexture.SetPixel(x, y, c);
        }

        generatedTexture.Apply();

        // PNG 변환
        byte[] bytes = generatedTexture.EncodeToPNG();

        // 디렉토리 자동 생성
        string directory = Path.GetDirectoryName(newSavePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 파일 저장
        File.WriteAllBytes(newSavePath, bytes);
        Debug.Log($"Gradient texture saved to: {newSavePath}");

        // Unity 에디터에 반영
        AssetDatabase.ImportAsset(newSavePath);
        AssetDatabase.Refresh();

        EnableReadWrite(newSavePath);  // Read/Write 활성화
        SetTextureImporterOptions(newSavePath);

        // 저장된 텍스처를 새로 로드
        Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newSavePath);
        if (importedTexture != null)
            generatedTexture = importedTexture;
    }

    public void EnableReadWrite(string assetPath)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (textureImporter != null && !textureImporter.isReadable)
        {
            textureImporter.isReadable = true;
            textureImporter.SaveAndReimport();
            Debug.Log("Read/Write Enabled for texture at: " + assetPath);
        }
    }

    public void SetTextureImporterOptions(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.mipmapEnabled = false;             // MipMaps 비활성화
            importer.textureCompression = TextureImporterCompression.Uncompressed;  // 압축 해제 (None)
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
#endif
}