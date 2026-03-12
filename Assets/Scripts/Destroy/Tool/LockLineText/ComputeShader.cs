using UnityEngine;

public class ComputeShader
{
    private static readonly int widthRatio = Shader.PropertyToID("widthRatio");
    private static readonly int heightRatio = Shader.PropertyToID("heightRatio");
    private static readonly UnityEngine.ComputeShader computeShader = Resources.Load<UnityEngine.ComputeShader>("ComputeShader/CompressImage");
    public static RenderTexture CompressImage(Texture2D target, int targetWidth = 256, int targetHeight = 256)
    {
        RenderTexture renderTexture = new RenderTexture(targetWidth, targetHeight, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        //ComputeShader computeShader = Resources.Load<ComputeShader>("CompressImage");
        int kernelIndex = computeShader.FindKernel("CompressImage");
        computeShader.SetFloat(widthRatio, target.width / (float)targetWidth);
        computeShader.SetFloat(heightRatio, target.height / (float)targetHeight);
        computeShader.SetTexture(kernelIndex, "oldImage", target);
        computeShader.SetTexture(kernelIndex, "newImage", renderTexture);
        computeShader.Dispatch(kernelIndex, targetWidth / 16, targetHeight / 16, 1);
        return renderTexture;
    }
}

