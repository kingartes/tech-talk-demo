using System;
using System.Globalization;
using UnityEngine;

namespace TechTalk
{
    internal sealed class OverlayUI
    {
        internal static readonly Color Ink = new(.88f,.94f,1);
        internal static readonly Color Muted = new(.63f,.74f,.83f);
        internal static readonly Color Cyan = new(.37f,.88f,.99f);
        internal static readonly Color Amber = new(1,.72f,.31f);
        internal static readonly Color Red = new(1,.36f,.24f);
        private static readonly Color Panel = new(.035f,.064f,.099f,.96f);
        private static readonly Color Rule = new(.20f,.32f,.41f);
        private readonly DemoStateController demo;
        private readonly GUIStyle title = new(), normal = new(), small = new(), number = new(), button = new();
        private bool ready;

        public OverlayUI(DemoStateController demo) => this.demo=demo;

        public void Draw()
        {
            if(!ready)
            {
                Setup(title,44,true);
                Setup(normal,24,false);
                Setup(small,19,false);
                Setup(number,41,true);
                button.fontSize=19;
                button.fontStyle=FontStyle.Bold;
                button.alignment=TextAnchor.MiddleCenter;
                button.normal.textColor=Ink;
                ready=true;
            }
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            Matrix4x4 previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),
                Quaternion.identity,new Vector3(scale,scale,1));
            int state=demo.State;
            Fill(new(0,0,1600,139),Panel);
            Fill(new(38,137,1524,1),Rule);
            Label(new(40,14,850,30),$"GRAPHICS PIPELINE     /     AX-17 TURBINE LAB     /     S{state:00}",small,Cyan);
            Label(new(38,42,1060,60),DemoStateController.Titles[state],title,Ink);
            Label(new(41,101,1350,31),DemoStateController.Descriptions[state],small,Muted);
            Label(new(1351,53,212,31),demo.Paused ? "CLOCK PAUSED" : "CLOCK RUNNING",small,demo.Paused?Amber:Muted);

            Fill(new(37,171,433,571),Panel);
            if(state==2) TransformPanel();
            else if(state==4)
            {
                demo.Raster.Draw(demo.Step);
                RasterPanel();
            }
            else if(state==5 || state==6) MaterialPanel();
            else TurbinePanel(state);

            Fill(new(0,766,1600,134),Panel);
            Fill(new(38,767,1524,1),Rule);
            if(!demo.Clean)
            {
                for(int i=0;i<10;i++)
                {
                    string[] shortNames={"Final","Mesh","Spaces","Vertex","Raster","UV","Light","Heat","X-Ray","Fault"};
                    if(Button(new(39+i*153,784,143,38),$"{i}  {shortNames[i]}",i==state)) demo.Select(i);
                }
                if(Button(new(39,836,83,40),"‹ Prev")) demo.Select(state-1);
                if(Button(new(130,836,86,40),"Next ›")) demo.Select(state+1);
                if(Button(new(224,836,136,40),"Step  Tab")) demo.CycleStep();
                if(Button(new(368,836,162,40),demo.Paused ? "Play  Space" : "Pause  Space")) demo.TogglePause();
                if(Button(new(538,836,102,40),"Reset  R")) demo.ResetState();
                if(Button(new(648,836,144,40),"Reset all  ⇧R")) demo.ResetAll();
                if(Button(new(800,836,132,40),"Opening  I")) demo.PlayOpening();
                if(Button(new(940,836,117,40),"Orbit  O",demo.Orbit)) demo.Orbit=!demo.Orbit;
                if(Button(new(1065,836,151,40),"Start fault  F")) demo.StartFailure();
                if(Button(new(1224,836,164,40),"Final  Home")) demo.Select(0);
                if(Button(new(1396,836,163,40),"Clean view  H")) demo.Clean=true;
            }
            else
            {
                Label(new(40,793,1000,38),"← / →   STATES      TAB   STEP      SPACE   PAUSE      F   REPLAY FAILURE",normal,Muted);
                if(Button(new(1396,818,163,40),"Controls  H")) demo.Clean=false;
            }
            GUI.matrix=previous;
        }

        private void TurbinePanel(int state)
        {
            float c=state==7?demo.Temperature:demo.Current.Celsius;
            Caption(193,"SYNTHETIC TELEMETRY");
            Reading(222,English($"{c:0.0} °C"),"Sensor T-04  /  hot section",state==9 && demo.Current.Severity>.5f ? Red:Ink);
            Reading(322,English($"{demo.Current.Vibration:0.0} mm/s"),"Vibration  /  RMS",Ink);
            Reading(422,English($"{demo.Current.Rpm:0} rpm"),"Rotor speed  /  simulated",Ink);
            Fill(new(57,519,390,1),Rule);
            switch(state)
            {
                case 0:
                    Caption(538,"AX-17  /  STUDIO VIEW");
                    Paragraph(573,"Orbit the camera or run the 40-second opening sequence.");
                    break;
                case 1:
                    Caption(538,"MESH TOPOLOGY");
                    Paragraph(573,English($"{demo.TriangleCount:N0} turbine triangles. Rotor angle and camera are held at the inspection preset."));
                    break;
                case 3:
                    Caption(538,demo.Step==0?"BEFORE  /  0.00 SCENE UNITS":"AFTER  /  0.19 SCENE UNITS");
                    if(Button(new(57,579,389,40),demo.Step==0?"Enable vertex wave  ·  Tab":"Disable vertex wave  ·  Tab",true)) demo.CycleStep();
                    Paragraph(636,"Exaggerated traveling wave on casing and rotor.");
                    break;
                case 7:
                    Caption(538,"THERMAL SCALE  /  CELSIUS");
                    Gradient(57,579);
                    if(!demo.Clean) demo.Temperature=Slider(649,"Sensor input",demo.Temperature,35,140," °C");
                    break;
                case 8:
                    Caption(538,"TRANSPARENT CASING");
                    Paragraph(573,"Depth test on. Depth writes off. The opaque rotor draws first.");
                    if(!demo.Clean) demo.Opacity=Slider(653,"Face opacity",demo.Opacity,.02f,.78f,"");
                    break;
                case 9:
                    bool warning=demo.Current.Severity>.48f;
                    Caption(538,warning?"WARNING  /  HOT SECTION":"SIMULATED FAILURE  /  PRESET");
                    if(warning)
                    {
                        Fill(new(484,675,1075,62),new Color(.25f,.06f,.055f,.96f));
                        Label(new(504,684,1030,44),"ALARM   ·   RISING TEMPERATURE AND VIBRATION",normal,Red);
                    }
                    if(Button(new(57,579,389,41),demo.Failure.Running?"Replay failure  ·  F":"Start failure  ·  F",true)) demo.StartFailure();
                    Paragraph(640,English($"Preset time  {demo.Failure.Time:0.0} s  /  16 s"));
                    break;
            }
        }

        private void TransformPanel()
        {
            Caption(193,"MODEL TRANSFORM");
            if(!demo.Clean)
            {
                demo.Move=Slider(229,"Translate X",demo.Move,-1.2f,1.2f,"");
                demo.Turn=Slider(298,"Rotate Y",demo.Turn,-180,180,"°");
                demo.Size=Slider(367,"Uniform scale",demo.Size,.55f,2.5f,"×");
            }
            Vector3 p=new(.5f,.5f,-.5f);
            Vector3 w=demo.Cube.TransformPoint(p);
            Vector3 v=demo.Camera.worldToCameraMatrix.MultiplyPoint(w);
            Matrix4x4 projection=GL.GetGPUProjectionMatrix(demo.Camera.projectionMatrix,false);
            Vector4 clip=projection * new Vector4(v.x,v.y,v.z,1);
            Caption(445,"TRACKED CORNER  /  GPU CLIP");
            Label(new(57,483,398,32),$"Object    {Format(p)}",small,Ink);
            Label(new(57,522,398,32),$"World     {Format(w)}",small,Ink);
            Label(new(57,561,398,32),$"View        {Format(v)}",small,Ink);
            Label(new(57,600,398,32),$"Clip         {Format(clip)}",small,Ink);
            Label(new(57,639,398,32),$"NDC        {Format((Vector3)clip/clip.w)}",small,Cyan);
            DrawAxes();
            Vector2 pixel=Project(w);
            Fill(new(pixel.x-6,pixel.y-6,12,12),Amber);
        }

        private void DrawAxes()
        {
            Vector2 origin=Project(Vector3.zero);
            Axis(origin,Vector3.right,"X",Red);
            Axis(origin,Vector3.up*.74f,"Y",new Color(.45f,1,.56f));
            Axis(origin,Vector3.forward,"Z",Cyan);
        }

        private void Axis(Vector2 origin,Vector3 direction,string name,Color color)
        {
            Vector2 end=Project(direction*1.82f);
            Line(origin,end,3,color);
            Label(new(end.x+6,end.y-20,40,37),name,normal,color);
        }

        private void RasterPanel()
        {
            string[] steps={"01  /  Triangle setup","02  /  Sample coverage","03  /  RGB interpolation","04  /  UV interpolation"};
            Caption(193,steps[demo.Step]);
            Paragraph(230,"One sample per cell at its center. Pick a cell in the grid.");
            if(Button(new(57,322,389,43),"Next raster step  ·  Tab",true)) demo.CycleStep();
            var info=demo.Raster.Info;
            Fill(new(57,392,390,1),Rule);
            Caption(420,info.Inside?"SAMPLE COVERED":"SAMPLE OUTSIDE");
            Paragraph(459,$"Grid cell  ({info.X}, {info.Y})");
            if(info.Inside && demo.Step>=2)
            {
                Paragraph(509,$"Weights  {Format(info.Weights)}");
                Paragraph(560,English($"UV  ({info.UV.x:0.00}, {info.UV.y:0.00})"));
            }
            Paragraph(644,"Affine screen-space model. Equal clip w at all three corners.");
        }

        private void MaterialPanel()
        {
            string[] labels=demo.State==5?new[]{"Solid color","UV as color","Sampled texture"}:
                new[]{"Unlit base color","World normals as RGB","Lambert diffuse"};
            Caption(193,"SAME MESH  /  DIFFERENT OUTPUT");
            for(int i=0;i<labels.Length;i++)
                if(Button(new(57,239+i*58,389,42),labels[i],demo.Step==i))
                    while(demo.Step!=i) demo.CycleStep();
            Paragraph(437,demo.State==5 ?
                "U becomes red and V becomes green. The checker is sampled from a generated texture." :
                "Normal RGB remaps −1…1 into 0…1. Lambert scales the base color with max(dot(N,L),0).");
            if(demo.State==6)
            {
                if(!demo.Clean) demo.LightAngle=Slider(565,"Light azimuth",demo.LightAngle,-180,180,"°");
                Vector2 a=Project(demo.LightDirection*2.2f), b=Project(demo.LightDirection*1.48f);
                Line(a,b,4,Amber);
                Label(new(a.x+8,a.y-12,75,32),"LIGHT",small,Amber);
            }
        }

        private Vector2 Project(Vector3 position)
        {
            Vector3 point=demo.Camera.WorldToScreenPoint(position);
            float s=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            return new((point.x-(Screen.width-1600*s)/2)/s,(Screen.height-point.y-(Screen.height-900*s)/2)/s);
        }

        private static string Format(Vector3 v) => English($"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})");
        private static string Format(Vector4 v) => English($"({v.x:0.00}, {v.y:0.00}, {v.z:0.00}, {v.w:0.00})");
        private static string English(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

        private void Gradient(int x,int y)
        {
            for(int i=0;i<120;i++)
            {
                float t=i/119f;
                Color a = new(.025f,.14f,.74f), b = new(0,.76f,.83f), c = new(1,.79f,.07f), d = new(.94f,.048f,.02f);
                Color col=t<1f/3?Color.Lerp(a,b,t*3):t<2f/3?Color.Lerp(b,c,t*3-1):Color.Lerp(c,d,t*3-2);
                Fill(new(x+i*3,y,3,16),col.gamma);
            }
            Label(new(x,y+18,75,26),"30 °C",small,Muted);
            Label(new(x+153,y+18,75,26),"85 °C",small,Muted);
            Label(new(x+304,y+18,85,26),"140 °C",small,Muted);
        }

        private void Reading(int y,string value,string subtitle,Color color)
        {
            Label(new(56,y,390,50),value,number,color);
            Label(new(57,y+51,391,27),subtitle,small,Muted);
        }
        private void Caption(int y,string text) => Label(new(57,y,392,30),text,small,Cyan);
        private void Paragraph(int y,string text) => Label(new(57,y,388,85),text,normal,Muted);
        private float Slider(int y,string title,float value,float min,float max,string unit)
        {
            Label(new(57,y,391,29),English($"{title}  {value:0.00}{unit}"),small,Ink);
            return GUI.HorizontalSlider(new(60,y+35,375,22),value,min,max);
        }

        private static void Setup(GUIStyle style,int size,bool bold)
        {
            style.fontSize=size;
            style.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            style.wordWrap=true;
            style.alignment=TextAnchor.UpperLeft;
        }
        internal static void Fill(Rect rect,Color color)
        {
            if(Event.current.type!=EventType.Repaint) return;
            Color old=GUI.color;
            GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=old;
        }
        internal static void Label(Rect rect,string value,GUIStyle style,Color color)
        {
            style.normal.textColor=color;
            GUI.Label(rect,value,style);
        }
        internal static void Line(Vector2 a,Vector2 b,float width,Color color)
        {
            if(Event.current.type!=EventType.Repaint) return;
            Matrix4x4 old=GUI.matrix;
            float angle=Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
            GUI.matrix=old * Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
            Fill(new(0,-width/2,(b-a).magnitude,width),color);
            GUI.matrix=old;
        }
        private bool Button(Rect rect,string label,bool selected=false)
        {
            bool hover=rect.Contains(Event.current.mousePosition);
            Fill(rect,selected?new Color(.11f,.33f,.43f):hover?new Color(.14f,.25f,.33f):new Color(.08f,.15f,.21f));
            return GUI.Button(rect,label,button);
        }
    }
}
