using UnityEngine;
using static TechTalk.OverlayUI;

namespace TechTalk
{
    internal sealed class RasterizationLab
    {
        private const int Columns=23, Rows=13, Cell=40, Left=548, Top=189;
        private static readonly Vector2 A=new(3.2f,10.7f), B=new(11.2f,1.15f), C=new(20.5f,10.4f);
        private readonly GUIStyle label=new(){fontSize=22,fontStyle=FontStyle.Bold};
        private int selectedX=11, selectedY=7;
        internal (int X,int Y,bool Inside,Vector3 Weights,Vector2 UV) Info => Evaluate(selectedX,selectedY);
        internal void Reset() { selectedX=11; selectedY=7; }

        private static float Cross(Vector2 p,Vector2 q) => p.x*q.y-p.y*q.x;
        private static (int,int,bool,Vector3,Vector2) Evaluate(int x,int y)
        {
            Vector2 p=new(x+.5f,y+.5f);
            float total=Cross(B-A,C-A);
            float b=Cross(p-A,C-A)/total;
            float c=Cross(B-A,p-A)/total;
            float a=1-b-c;
            // None of this grid's sample centers lies exactly on an edge.
            bool inside=a>=0 && b>=0 && c>=0;
            return (x,y,inside,new Vector3(a,b,c),new Vector2(c+b*.5f,b));
        }

        internal void Draw(int step)
        {
            Event e=Event.current;
            if(e.type==EventType.MouseDown && e.button==0 && new Rect(Left,Top,Columns*Cell,Rows*Cell).Contains(e.mousePosition))
            {
                selectedX=(int)((e.mousePosition.x-Left)/Cell);
                selectedY=(int)((e.mousePosition.y-Top)/Cell);
                e.Use();
            }
            if(e.type!=EventType.Repaint) return;
            for(int y=0;y<Rows;y++)
                for(int x=0;x<Columns;x++)
                {
                    var (_,_,inside,weights,uv)=Evaluate(x,y);
                    Color color=new(.075f,.116f,.164f);
                    if(inside)
                    {
                        if(step==1) color=new(.085f,.48f,.56f);
                        if(step==2) color=new(weights.x,weights.y,weights.z);
                        if(step==3) color=new(uv.x,uv.y,.20f);
                    }
                    float px=Left+x*Cell, py=Top+y*Cell;
                    Fill(new(px,py,Cell-2,Cell-2),color);
                    if(step<=1) Fill(new(px+18,py+18,4,4),inside&&step==1?Ink:Muted);
                    if(x==selectedX && y==selectedY)
                    {
                        Fill(new(px,py,Cell,3),Amber);
                        Fill(new(px,py+Cell-3,Cell,3),Amber);
                        Fill(new(px,py,3,Cell),Amber);
                        Fill(new(px+Cell-3,py,3,Cell),Amber);
                    }
                }
            Vector2 pa=At(A), pb=At(B), pc=At(C);
            Line(pa,pb,2,Ink); Line(pb,pc,2,Ink); Line(pc,pa,2,Ink);
            Vertex(pa,"A  (0,0)",Red,-83,4);
            Vertex(pb,"B  (.5,1)",new Color(.49f,1,.58f),11,-10);
            Vertex(pc,"C  (1,0)",Cyan,9,4);
        }

        private void Vertex(Vector2 p,string name,Color color,int x,int y)
        {
            Fill(new(p.x-5,p.y-5,10,10),color);
            Label(new(p.x+x,p.y+y,110,40),name,label,color);
        }
        private static Vector2 At(Vector2 p) => new(Left+p.x*Cell,Top+p.y*Cell);
    }
}
