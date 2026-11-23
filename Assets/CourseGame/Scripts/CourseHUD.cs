using UnityEngine;
using UnityEngine.UI;

namespace CourseGame
{
    public sealed class CourseHUD : MonoBehaviour
    {
        static readonly Color Ink = new(.045f,.05f,.09f,.96f);
        static readonly Color Accent = new(.98f,.46f,.24f);
        static readonly Color Blue = new(.42f,.55f,1f);
        Font font;
        public Canvas playerCanvas;
        public Text killText;
        GUIStyle label, button;
        void Awake()
        {
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft JhengHei","Arial"},24);
            if(playerCanvas)foreach(var text in playerCanvas.GetComponentsInChildren<Text>(true))text.font=font;
        }
        void LateUpdate()
        {
            var s=CourseSession.Instance;
            if(playerCanvas)playerCanvas.enabled=s && s.State!=CourseSession.Phase.Welcome;
            if(s && killText)killText.text=$"擊殺  {s.Kills:000}";
        }
        void OnGUI()
        {
            var s=CourseSession.Instance; if(!s) return;
            if(label==null)
            {
                label = new GUIStyle(GUI.skin.label) { font=font, wordWrap=true, richText=true };
                button = new GUIStyle(GUI.skin.button) { font=font, fontSize=19, alignment=TextAnchor.MiddleCenter };
                button.normal.textColor=Color.white;
            }
            float scale=Mathf.Min(Screen.width/1440f, Screen.height/900f);
            float ox=(Screen.width-1440*scale)*.5f, oy=(Screen.height-900*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            Panel(new Rect(30,25,305,105));
            Text(new Rect(50,35,260,27),"LAST SIGNAL  ／  最後訊號",16,Blue);
            Text(new Rect(50,65,260,42),"生存演練",27,Color.white);
            if(s.State != CourseSession.Phase.Welcome)
            {
                Panel(new Rect(490,25,460,84));
                float remaining=Mathf.Max(0,s.duration-s.Elapsed);
                Text(new Rect(510,36,420,45),$"撤離倒數  {Mathf.FloorToInt(remaining/60):00}:{Mathf.FloorToInt(remaining%60):00}",29,Color.white,TextAnchor.MiddleCenter);
                Bar(new Rect(510,88,420,4),s.Elapsed/s.duration,Blue);
            }
            Panel(new Rect(30,840,1380,40));
            Text(new Rect(48,847,1140,25),"W A S D  移動     ／     自動射擊     ／     ESC  暫停     ／     R  重新開始     ／     M  聲音",15,new Color(.76f,.78f,.86f));
            Text(new Rect(1220,847,170,25),s.Muted?"聲音：關閉":"聲音：開啟",15,Blue,TextAnchor.MiddleRight);
            if(s.State==CourseSession.Phase.Playing && Time.unscaledTime < s.NoticeUntil)
            {
                Panel(new Rect(380,760,680,49));
                Text(new Rect(390,771,660,27),s.Notice,19,Color.white,TextAnchor.MiddleCenter);
            }
            if(s.State==CourseSession.Phase.Playing) { GUI.matrix=Matrix4x4.identity; return; }
            Fill(new Rect(0,0,1440,900),new Color(.025f,.025f,.06f,.69f));
            Panel(new Rect(385,185,670,530));
            Fill(new Rect(385,185,670,5),Accent);
            Text(new Rect(430,226,580,35),"L A S T   S I G N A L",23,Blue,TextAnchor.MiddleCenter);
            string title=s.State switch { CourseSession.Phase.Welcome=>"最後訊號",CourseSession.Phase.Paused=>"演練暫停",CourseSession.Phase.Victory=>"撤離成功",_=>"演練結束" };
            Text(new Rect(430,277,580,70),title,46,Color.white,TextAnchor.MiddleCenter);
            string body=s.State switch
            {
                CourseSession.Phase.Welcome=>"在無限延伸的基地中生存 3 分鐘。\n武器自動鎖定敵人，移動時留意障礙物。\n升級時暫停，選擇生命、傷害或移動強化。",
                CourseSession.Phase.Paused=>"喘一口氣。\n按 ESC 或下方按鈕繼續演練。",
                _=>$"生存時間  {Mathf.FloorToInt(s.Elapsed/60):00}:{Mathf.FloorToInt(s.Elapsed%60):00}\n擊殺  {s.Kills}    等級  {s.Level}    發射  {s.Shots}\n最佳擊殺紀錄  {s.BestKills}"
            };
            Text(new Rect(448,370,544,132),body,21,new Color(.8f,.82f,.9f),TextAnchor.MiddleCenter);
            GUI.backgroundColor=Accent;
            if(GUI.Button(new Rect(475,543,490,62),s.State==CourseSession.Phase.Welcome?"開始演練  /  ENTER":s.State==CourseSession.Phase.Paused?"繼續演練  /  ESC":"再次挑戰  /  R",button))
            { if(s.State==CourseSession.Phase.Welcome)s.StartRun();else if(s.State==CourseSession.Phase.Paused)s.Resume();else s.Restart(); }
            GUI.backgroundColor=Color.white;
            Text(new Rect(430,647,580,30),"保持移動，等待撤離訊號",14,Blue,TextAnchor.MiddleCenter);
            GUI.matrix=Matrix4x4.identity;
        }
        void Panel(Rect r) { Fill(r,Ink); }
        void Bar(Rect r,float v,Color c) { Fill(r,new Color(.16f,.17f,.25f)); r.width*=Mathf.Clamp01(v);Fill(r,c); }
        void Fill(Rect r,Color c) { var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old; }
        void Text(Rect r,string t,int size,Color c,TextAnchor align=TextAnchor.UpperLeft)
        { label.fontSize=size;label.normal.textColor=c;label.alignment=align;GUI.Label(r,t,label); }
    }
}
