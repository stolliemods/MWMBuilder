using Assimp;
using System.Collections.Generic;
using System.IO;
using VRageMath;
using VRageRender.Import;

namespace MwmBuilder
{
    public class MyMeshPartSolver
    {
        public static string ColorMetalSuffix = "_cm.dds";
        public static string NormalGlossSuffix = "_ng.dds";
        public static string AddMapsSuffix = "_add.dds";
        public static string AlphamaskSuffix = "_alphamask.dds";
        private Dictionary<int, MyMeshPartInfo> m_partContainer = new Dictionary<int, MyMeshPartInfo>();

        public Dictionary<int, MyMeshPartInfo> GetMeshPartContainer()
        {
            return this.m_partContainer;
        }

        public void SetMaterial(Material material)
        {
            if (material.GetMaterialTextureCount(TextureType.Diffuse) == 0)
                return;
            int hashCode = material.Name.GetHashCode();
            if (!this.m_partContainer.ContainsKey(hashCode))
                return;
            MyMeshPartInfo myMeshPartInfo = this.m_partContainer[hashCode];
            MyMaterialDescriptor matDesc = new MyMaterialDescriptor(material.Name);
            this.SetMaterialTextures(matDesc, material);
            if (myMeshPartInfo.m_MaterialDesc != null)
                matDesc.Technique = myMeshPartInfo.m_MaterialDesc.Technique;
            myMeshPartInfo.m_MaterialDesc = matDesc;
        }

        private void SetMaterialTextures(MyMaterialDescriptor matDesc, Material material)
        {
            TextureSlot texture;
            material.GetMaterialTexture(TextureType.Diffuse, 0, out texture);
            string filePath = texture.FilePath;
            if (filePath.Length < MyMeshPartSolver.ColorMetalSuffix.Length)
                return;
            string str = filePath.Substring(0, filePath.Length - MyMeshPartSolver.ColorMetalSuffix.Length);
            try
            {
                string path2 = MyModelProcessor.GetResourcePathInContent(str + MyMeshPartSolver.ColorMetalSuffix).TrimStart('\\', '/');
                if (File.Exists(Path.Combine(ProgramContext.OutputDir, path2)))
                    matDesc.Textures.Add("ColorMetalTexture", path2);
            }
            catch
            {
            }
            try
            {
                string path2 = MyModelProcessor.GetResourcePathInContent(str + MyMeshPartSolver.NormalGlossSuffix).TrimStart('\\', '/');
                if (File.Exists(Path.Combine(ProgramContext.OutputDir, path2)))
                    matDesc.Textures.Add("NormalGlossTexture", path2);
            }
            catch
            {
            }
            try
            {
                string path2 = MyModelProcessor.GetResourcePathInContent(str + MyMeshPartSolver.AddMapsSuffix).TrimStart('\\', '/');
                if (File.Exists(Path.Combine(ProgramContext.OutputDir, path2)))
                    matDesc.Textures.Add("AddMapsTexture", path2);
            }
            catch
            {
            }
            try
            {
                string path2 = MyModelProcessor.GetResourcePathInContent(str + MyMeshPartSolver.AlphamaskSuffix).TrimStart('\\', '/');
                if (!File.Exists(Path.Combine(ProgramContext.OutputDir, path2)))
                    return;
                matDesc.Textures.Add("AlphamaskTexture", path2);
            }
            catch
            {
            }
        }

        public void SetIndices(
          Assimp.Mesh sourceMesh,
          VRageRender.Import.Mesh mesh,
          int[] indices,
          List<Vector3> vertices,
          int matHash)
        {
            MyMeshPartInfo myMeshPartInfo;
            if (!this.m_partContainer.TryGetValue(matHash, out myMeshPartInfo))
            {
                myMeshPartInfo = new MyMeshPartInfo();
                myMeshPartInfo.m_MaterialHash = matHash;
                this.m_partContainer.Add(matHash, myMeshPartInfo);
            }
            mesh.StartIndex = myMeshPartInfo.m_indices.Count;
            mesh.IndexCount = indices.Length;
            int vertexOffset = mesh.VertexOffset;
            for (int index = 0; index < sourceMesh.FaceCount * 3; index += 3)
            {
                int num1 = indices[index] + vertexOffset;
                int num2 = indices[index + 1] + vertexOffset;
                int num3 = indices[index + 2] + vertexOffset;
                myMeshPartInfo.m_indices.Add(num1);
                myMeshPartInfo.m_indices.Add(num2);
                myMeshPartInfo.m_indices.Add(num3);
            }
        }

        public void Clear()
        {
            this.m_partContainer.Clear();
        }
    }
}
