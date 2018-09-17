using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SceneViewOutline : MonoBehaviour
{
    public Renderer[] Renderers;
    public Color OutlineColor;
    public bool halfBufferSize;

    private CommandBuffer m_CommandBuffer = null;
    private Material m_OutlineMaterial = null;

    public void SetupCommandBuffer(Renderer[] renderers)
    {
        if (m_CommandBuffer == null)
            m_CommandBuffer = new CommandBuffer();
        if (m_OutlineMaterial == null)
            m_OutlineMaterial = new Material(Shader.Find("Hidden/SceneViewOutline"));
        m_CommandBuffer.Clear();

        int id = 1;
        Renderers = renderers;
        foreach (var r in Renderers.OrderBy(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID)).ThenBy(r => r.sortingOrder))
        {
            if (r == null) continue;
            int meshcount = 1;
            if (r is MeshRenderer)
            {
                var f = r.GetComponent<MeshFilter>();
                if (f != null && f.sharedMesh != null)
                    meshcount = f.sharedMesh.subMeshCount;
            }
            if (r is SkinnedMeshRenderer && (r as SkinnedMeshRenderer).sharedMesh != null)
                meshcount = (r as SkinnedMeshRenderer).sharedMesh.subMeshCount;

            var col = new Color32((byte)(id & 0xff), (byte)((id >> 8) & 0xff), 0, 0);
            m_CommandBuffer.SetGlobalColor("_ID", col);
            for (int i = 0; i < meshcount; i++)
                m_CommandBuffer.DrawRenderer(r, m_OutlineMaterial, i, 0);
            id++;
        }
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (source == null || destination == null) return;
        if (m_CommandBuffer == null)
            m_CommandBuffer = new CommandBuffer();
        if (m_OutlineMaterial == null)
            m_OutlineMaterial = new Material(Shader.Find("Hidden/SceneViewOutline"));
        if (Renderers == null || Renderers.Length == 0)
        {
            m_CommandBuffer.Clear();
            Graphics.Blit(source, destination);
            return;
        }

        var width = halfBufferSize ? source.width / 2 : source.width;
        var height = halfBufferSize ? source.height / 2 : source.height;
        var rt1 = RenderTexture.GetTemporary(width, height, 32);
        var rt2 = RenderTexture.GetTemporary(width, height, 32);
        var rt3 = RenderTexture.GetTemporary(width, height, 32);

        RenderTexture.active = rt1;
        GL.Clear(true, true, Color.clear);

        Graphics.ExecuteCommandBuffer(m_CommandBuffer);
        RenderTexture.active = null;

        Graphics.Blit(rt1, rt2, m_OutlineMaterial, 1);
        //Graphics.Blit(rt1, destination);

        m_OutlineMaterial.SetVector("_Direction", new Vector2(1, 0));
        Graphics.Blit(rt2, rt3, m_OutlineMaterial, 2);
        m_OutlineMaterial.SetVector("_Direction", new Vector2(0, 1));
        Graphics.Blit(rt3, rt1, m_OutlineMaterial, 2);

        m_OutlineMaterial.SetTexture("_OutlineMap", rt1);
        m_OutlineMaterial.SetTexture("_FillMap", rt2);
        m_OutlineMaterial.SetColor("_OutlineColor", OutlineColor);
        Graphics.Blit(source, destination, m_OutlineMaterial, 3);

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
        RenderTexture.ReleaseTemporary(rt3);
    }
}