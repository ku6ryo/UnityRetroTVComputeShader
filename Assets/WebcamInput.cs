using UnityEngine;
using UnityEngine.UI;

public class WebcamInput : MonoBehaviour
{
    struct ThreadSize
    {
      public uint x;
      public uint y;
      public uint z;

      public ThreadSize(uint x, uint y, uint z)
      {
        this.x = x;
        this.y = y;
        this.z = z;
      }
    }

    [SerializeField] Vector2Int _resolution = new Vector2Int(1024, 1024);
    [SerializeField] RawImage processedImage;
    [SerializeField] ComputeShader shader;

    WebCamTexture _webcamTexture;
    RenderTexture _tmpRenderTexture;

    void Start()
    {
        _webcamTexture = new WebCamTexture("", _resolution.x, _resolution.y);
        _tmpRenderTexture = new RenderTexture(_resolution.x, _resolution.y, 0);
        _tmpRenderTexture.enableRandomWrite = true;
        _tmpRenderTexture.Create();
        _webcamTexture.Play();
    }

    void OnDestroy()
    {
        if (_webcamTexture != null) Destroy(_webcamTexture);
        if (_tmpRenderTexture != null) Destroy(_tmpRenderTexture);
    }

    void ApplyShader(Texture source, RenderTexture result)
    {
        var kernelIndex = shader.FindKernel("CSMain");
        ThreadSize threadSize = new ThreadSize();
        shader.GetKernelThreadGroupSizes(kernelIndex, out threadSize.x, out threadSize.y, out threadSize.z);
        shader.SetTexture(kernelIndex, "Input", source);
        shader.SetTexture(kernelIndex, "Result", result);
        shader.SetFloat("xIntensity", Mathf.Sin(Random.Range(0f, 1f) * 10) * 10);
        shader.SetFloat("time", Time.time);
        shader.Dispatch(
          kernelIndex,
          _resolution.x / (int) threadSize.x,
          _resolution.y / (int) threadSize.y,
          (int) threadSize.z
        );
    }

    void Update()
    {
        if (!_webcamTexture.didUpdateThisFrame) return;

        var aspect1 = (float)_webcamTexture.width / _webcamTexture.height;
        var aspect2 = (float)_resolution.x / _resolution.y;
        var gap = aspect2 / aspect1;

        var vflip = _webcamTexture.videoVerticallyMirrored;
        var scale = new Vector2(gap, vflip ? -1 : 1);
        var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

        ApplyShader(_webcamTexture, _tmpRenderTexture);
        processedImage.texture = _tmpRenderTexture;
    }
}
