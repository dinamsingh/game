using UnityEngine;

namespace RoxxParking
{
    [RequireComponent(typeof(Camera))]
    public sealed class VisualFinish : MonoBehaviour
    {
        private Material finish;
        public bool ContactShading=true;
        public void SetQuality(bool high)
        {
            ContactShading=high;
            GetComponent<Camera>().depthTextureMode=high?DepthTextureMode.DepthNormals:DepthTextureMode.None;
        }
        private void OnEnable()
        {
            Shader shader=Resources.Load<Shader>("VisualFinish");
            if(shader!=null && shader.isSupported)finish=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            GetComponent<Camera>().depthTextureMode|=DepthTextureMode.DepthNormals;
        }
        private void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(finish==null){Graphics.Blit(source,destination);return;}
            if(!ContactShading){finish.SetTexture("_OcclusionTex",Texture2D.whiteTexture);Graphics.Blit(source,destination,finish,1);return;}
            RenderTexture ao=RenderTexture.GetTemporary(Mathf.Max(1,source.width/2),Mathf.Max(1,source.height/2),0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            try
            {
                finish.SetFloat("_Radius",.65f);finish.SetFloat("_ContactStrength",ContactShading?.55f:0);
                Graphics.Blit(source,ao,finish,0);
                finish.SetTexture("_OcclusionTex",ao);
                Graphics.Blit(source,destination,finish,1);
            }
            finally{RenderTexture.ReleaseTemporary(ao);}
        }
        private void OnDisable(){if(finish!=null)Destroy(finish);finish=null;}
    }
}
