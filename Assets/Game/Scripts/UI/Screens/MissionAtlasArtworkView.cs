using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Cover-crops the exact comic sprite subrect without stretching its characters.</summary>
    [ExecuteAlways, RequireComponent(typeof(RawImage))]
    public sealed class MissionAtlasArtworkView : MonoBehaviour
    {
        [SerializeField] private Sprite comic;
        [SerializeField] private Texture plate;
        [SerializeField] private Image containedIcon;
        private bool contain;
        public void AuthorIconSupport()
        {
            var go=new GameObject("ContainedIcon",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);MissionAtlasScreenView.Fill(go.transform as RectTransform);containedIcon=go.GetComponent<Image>();containedIcon.preserveAspect=true;containedIcon.raycastTarget=false;go.SetActive(false);
        }
        public void ContainIcon(bool value) {contain=value;Refresh();}
        private RawImage image;
        public RawImage Image => image!=null?image:image=GetComponent<RawImage>();
        public Sprite Comic => comic;
        public void Set(Sprite sprite,Texture texture) {comic=sprite;plate=texture;Refresh();}
        private void OnEnable()=>Refresh();
        private void OnRectTransformDimensionsChange()=>Refresh();
        public void Refresh()
        {
            var raw=Image;if(raw==null)return;raw.raycastTarget=false;
            Texture texture=comic!=null?comic.texture:plate;raw.texture=texture;raw.enabled=texture!=null&&!contain;
            if(containedIcon!=null){containedIcon.gameObject.SetActive(contain&&comic!=null);containedIcon.sprite=comic;}
            if(contain)return;
            if(texture==null)return;
            Rect uv=comic!=null?new Rect(comic.rect.x/texture.width,comic.rect.y/texture.height,comic.rect.width/texture.width,comic.rect.height/texture.height):new Rect(0,0,1,1);
            Vector2 size=raw.rectTransform.rect.size;if(size.x<=0||size.y<=0)return;
            float source=(texture.width*uv.width)/(texture.height*uv.height),target=size.x/size.y;
            if(source>target) {float width=uv.width*target/source;uv.x+=(uv.width-width)*.5f;uv.width=width;}
            else {float height=uv.height*source/target;uv.y+=(uv.height-height)*.5f;uv.height=height;}
            raw.uvRect=uv;
        }
    }
}
