using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TechTalk
{
    internal sealed class TurbineGeometry
    {
        public Transform Root { get; }
        public Transform Rotor { get; }
        public MeshRenderer Shell { get; }
        public MeshRenderer InspectionCover { get; }
        public Material ShellMaterial => steel;
        public readonly List<MeshRenderer> Parts = new();
        public int Triangles { get; private set; }

        private readonly Material steel, dark, trim, bronze, black;
        private readonly Vector3 axis = new(0, .18f, 0);

        public TurbineGeometry(Material surface)
        {
            Root = new GameObject("AX-17 / procedural turbine").transform;
            Rotor = new GameObject("Rotor stages").transform;
            Rotor.SetParent(Root, false);
            Rotor.localPosition = axis;
            steel = Tone(surface, "Satin steel", new(.45f,.53f,.63f), .88f);
            dark = Tone(surface, "Blue gunmetal", new(.15f,.22f,.30f), .7f);
            trim = Tone(surface, "Machined alloy", new(.66f,.74f,.81f), .96f);
            bronze = Tone(surface, "Copper alloy", new(.52f,.27f,.095f), .76f);
            black = Tone(surface, "Painted chassis", new(.08f,.14f,.20f), .16f);

            var casingProfile = new (float z,float r)[] {
                (-1.96f,1.11f),(-1.78f,1.20f),(-1.55f,1.25f),(-.8f,1.29f),
                (-.68f,1.40f),(.64f,1.40f),(.79f,1.25f),(1.55f,1.13f),(1.83f,.91f),
                (1.99f,.83f)
            };
            Shell = Add("Cutaway casing",Lathe(65,54,286,casingProfile),steel,Root,axis);
            InspectionCover = Add("Inspection casing cover",Lathe(35,-74,54,casingProfile),steel,Root,axis);
            Parts.Remove(InspectionCover);
            Triangles -= InspectionCover.GetComponent<MeshFilter>().sharedMesh.vertexCount/3;
            InspectionCover.gameObject.SetActive(false);
            foreach (float z in new[] {-1.85f, -.72f, .72f, 1.71f})
            {
                float r = z > 1 ? 1.11f : (Mathf.Abs(z) < 1 ? 1.42f : 1.24f);
                Add("Casing flange", Lathe(55,54,286,(z-.054f,r-.05f),(z-.045f,r+.053f),
                    (z+.045f,r+.053f),(z+.054f,r-.05f)), trim, Root, axis);
                for (int i=0; i<10; i++)
                {
                    float a = (59 + i * 24.5f) * Mathf.Deg2Rad;
                    var pos = axis + new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z-.062f);
                    Add("Flange fastener", Lathe(6,0,360,(-.052f,.05f),(0,.065f),(.045f,.065f)), dark, Root, pos);
                }
            }

            Add("Intake bell", Lathe(64,0,360,(-2.46f,.89f),(-2.48f,1.05f),(-2.40f,1.16f),
                (-2.29f,1.19f),(-2.12f,1.13f),(-1.96f,1.09f),(-1.96f,.92f),(-2.46f,.89f)), trim, Root, axis);
            Add("Exhaust duct", Lathe(60,0,360,(1.92f,.68f),(2.43f,.67f),(2.49f,.76f),
                (2.45f,.89f),(2.30f,.94f),(1.90f,.91f)), dark, Root, axis);
            Add("Drive shaft", Lathe(42,0,360,(-2.6f,0),(-2.53f,.14f),(-2.31f,.34f),(-2.12f,.40f),
                (-1.81f,.37f),(-1.34f,.41f),(-.7f,.37f),(0,.34f),(.55f,.39f),(1.15f,.34f),
                (1.8f,.32f),(2.20f,.27f),(2.25f,0)), bronze, Rotor, Vector3.zero);

            float[] stations = {-2.08f,-1.49f,-.88f,-.27f,.38f,1.01f,1.55f,1.99f};
            for (int i=0; i<stations.Length; i++)
            {
                float z = stations[i];
                float r = i < 5 ? .98f + i*.032f : 1.09f-(i-5)*.145f;
                Add("Rotor disk", Lathe(42,0,360,(z-.13f,.34f),(z-.09f,.52f),(z+.10f,.52f),
                    (z+.145f,.34f)), i % 2 == 0 ? dark : bronze, Rotor, Vector3.zero);
                Add($"Stage {i+1} blade row", BladeRow(r, z, 17+i, i%2==0 ? .44f : -.37f),
                    i < 4 ? trim : bronze, Rotor, Vector3.zero);
            }

            Box("Skid base", new(0,-1.49f,0), new(3.3f,.23f,5.6f), black);
            foreach (float x in new[] {-1.38f,1.38f})
            {
                Box("Skid rail", new(x,-1.32f,0), new(.18f,.18f,5.4f), trim);
                foreach(float z in new[] {-1.65f,1.55f})
                    Box("Bearing support", new(x*.59f,-1.06f,z), new(.32f,.55f,.58f), dark);
            }
            Pipe("Cooling manifold", .085f, bronze, new(-1.02f,1.14f,-1.64f),new(-1.21f,1.46f,-1.54f),
                new(-1.24f,1.54f,-1.36f),new(-1.24f,1.54f,.98f),new(-1.17f,1.44f,1.19f),new(-.86f,1.10f,1.29f));
            Pipe("Oil return", .07f, trim, new(.92f,-.59f,-1.45f),new(1.39f,-.68f,-1.38f),
                new(1.50f,-.81f,-1.27f),new(1.50f,-.81f,1.29f),new(1.42f,-.65f,1.44f),new(.91f,-.55f,1.54f));
            Pipe("Sensor conduit", .036f, bronze, new(1.08f,-.35f,.65f),new(1.46f,-.4f,.65f),
                new(1.61f,-.53f,.74f),new(1.61f,-.98f,.94f),new(1.48f,-1.11f,1.09f));
            Box("Sensor junction", new(1.42f,-1.05f,1.09f), new(.24f,.29f,.43f), bronze);
        }

        private void Box(string name, Vector3 p, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var source = obj.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(obj);
            var part = Add(name, source, material, Root, p, false);
            part.transform.localScale = scale;
        }

        private void Pipe(string name, float radius, Material mat, params Vector3[] path)
        {
            const int sides = 14;
            var positions = new List<Vector3>();
            var uv = new List<Vector2>();
            for (int i=0; i<path.Length; i++)
            {
                Vector3 dir = (path[Mathf.Min(i+1,path.Length-1)] - path[Mathf.Max(i-1,0)]).normalized;
                Vector3 a = Vector3.Cross(Vector3.up,dir).normalized;
                Vector3 b = Vector3.Cross(dir,a);
                for(int j=0; j<=sides; j++)
                {
                    float angle = j * Mathf.PI * 2 / sides;
                    positions.Add(path[i] + radius * (a*Mathf.Cos(angle) + b*Mathf.Sin(angle)));
                    uv.Add(new(j/(float)sides,i/(float)(path.Length-1)));
                }
            }
            Add(name, Grid(positions,uv,sides,path.Length), mat, Root, Vector3.zero);
        }

        private static Mesh Lathe(int sides, float start, float end, params (float z,float r)[] profile)
        {
            var positions = new List<Vector3>();
            var uv = new List<Vector2>();
            for(int i=0; i<profile.Length; i++)
                for(int j=0; j<=sides; j++)
                {
                    float u = j/(float)sides;
                    float a = Mathf.Lerp(start,end,u)*Mathf.Deg2Rad;
                    positions.Add(new(profile[i].r*Mathf.Cos(a), profile[i].r*Mathf.Sin(a), profile[i].z));
                    uv.Add(new(u,i/(float)(profile.Length-1)));
                }
            return Grid(positions,uv,sides,profile.Length);
        }

        private static Mesh Grid(List<Vector3> vertices, List<Vector2> uv, int columns, int rows)
        {
            var indices = new List<int>();
            for(int y=0;y<rows-1;y++)
                for(int x=0;x<columns;x++)
                {
                    int a=y*(columns+1)+x;
                    int b=a+columns+1;
                    indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                }
            return MeshFrom(vertices,uv,indices);
        }

        private static Mesh BladeRow(float tip, float z, int count, float twist)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var ids = new List<int>();
            // An airfoil-like section sweeps and tapers along the blade span.
            const int spans = 7, sides = 10;
            for(int blade=0;blade<count;blade++)
            {
                int start=v.Count;
                for(int i=0;i<=spans;i++)
                {
                    float span=i/(float)spans;
                    float r=Mathf.Lerp(.46f,tip,span);
                    float center=blade*2*Mathf.PI/count + twist*span*span;
                    for(int j=0;j<sides;j++)
                    {
                        float a=j*2*Mathf.PI/sides;
                        float chord=Mathf.Cos(a);
                        float tangential=Mathf.Sin(a)*(.065f-.038f*span) + chord*.042f;
                        float azimuth=center+tangential/r;
                        v.Add(new(r*Mathf.Cos(azimuth),r*Mathf.Sin(azimuth),
                            z+chord*(.15f-.039f*span)+.10f*span));
                        uv.Add(new(j/(float)sides,span));
                    }
                }
                for(int i=0;i<spans;i++)
                    for(int j=0;j<sides;j++)
                    {
                        int a=start+i*sides+j, b=start+i*sides+(j+1)%sides;
                        ids.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});
                    }
                int top=start+spans*sides;
                for(int j=1;j<sides-1;j++) ids.AddRange(new[]{top,top+j+1,top+j});
            }
            return MeshFrom(v,uv,ids);
        }

        private static Mesh MeshFrom(List<Vector3> vertices,List<Vector2> uv,List<int> ids)
        {
            var mesh=new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(ids,0);
            mesh.RecalculateNormals();
            return mesh;
        }

        private MeshRenderer Add(string name,Mesh source,Material material,Transform parent,Vector3 p,bool ownsSource=true)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=p;
            Mesh mesh=WithBarycentrics(source);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            Parts.Add(renderer);
            Triangles+=mesh.vertexCount/3;
            if(ownsSource) Object.Destroy(source);
            return renderer;
        }

        private static Mesh WithBarycentrics(Mesh source)
        {
            var ids=source.triangles;
            var v=source.vertices;
            var n=source.normals;
            var uv=source.uv;
            var vertices=new Vector3[ids.Length];
            var normals=new Vector3[ids.Length];
            var tex=new Vector2[ids.Length];
            var bary=new List<Vector3>(ids.Length);
            for(int i=0;i<ids.Length;i++)
            {
                int id=ids[i];
                vertices[i]=v[id];
                normals[i]=n[id];
                tex[i]=uv[id];
                bary.Add((i%3) switch { 0=>Vector3.right,1=>Vector3.up,_=>Vector3.forward });
                ids[i]=i;
            }
            var mesh=new Mesh { name=source.name+" / barycentric", vertices=vertices, normals=normals, uv=tex };
            mesh.SetUVs(1,bary);
            mesh.SetTriangles(ids,0);
            return mesh;
        }

        private static Material Tone(Material surface,string name,Color color,float metal)
        {
            var mat=new Material(surface) { name=name };
            mat.SetColor("_Color",color);
            mat.SetFloat("_Metal",metal);
            return mat;
        }
    }
}
