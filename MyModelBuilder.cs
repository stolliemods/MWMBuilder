using Assimp;
using Assimp.Configs;
using MwmBuilder.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VRage.FileSystem;
using VRageMath;
using VRageRender.Import;

namespace MwmBuilder
{
    public class MyModelBuilder
    {
        private Dictionary<string, Action<MyModelProcessor, object>> m_setters;

        private enum ExitCode : int
        {
            Success = 0,
            InvalidFBX = 1,
        }

        public MyModelBuilder()
        {
            this.InitSetters();
        }

        private void InitSetters()
        {
            this.m_setters = new Dictionary<string, Action<MyModelProcessor, object>>();
            foreach (PropertyInfo property in typeof(MyModelProcessor).GetProperties())
            {
                if (property.GetCustomAttributes(typeof(BrowsableAttribute), true).OfType<BrowsableAttribute>().Any<BrowsableAttribute>((Func<BrowsableAttribute, bool>)(s => s.Browsable)))
                    this.m_setters.Add(property.Name, MyModelBuilder.GetValueSetter<MyModelProcessor, object>(property));
            }
        }

        private static Action<T1, T2> GetValueSetter<T1, T2>(PropertyInfo propertyInfo)
        {
            MethodInfo method = typeof(MyParser).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(propertyInfo.PropertyType);
            ParameterExpression parameterExpression1;
            ParameterExpression parameterExpression2;
            return Expression.Lambda<Action<T1, T2>>((Expression)Expression.Call((Expression)
                (parameterExpression1 = Expression.Parameter(typeof(T1), "i")),
                propertyInfo.GetSetMethod(), (Expression)Expression.Convert((Expression)
                (parameterExpression2 = Expression.Parameter(typeof(T2), "a")),
                propertyInfo.PropertyType, method)), parameterExpression1, parameterExpression2).Compile();
        }

        public void Build(
          string filename,
          string intermediateDir,
          string outputDir,
          MyModelConfiguration configuration,
          byte[] havokCollisionShapes,
          bool checkOpenBoundaries,
          float[] lodDistances,
          bool overrideLods,
          Func<string, MyMaterialConfiguration> getMaterialByRef,
          IMyBuildLogger logger)
        {
            //logger.LogMessage(MessageType.Info, "**FileName: " + filename);
            
            string withoutExtension = Path.GetFileNameWithoutExtension(filename);
            //logger.LogMessage(MessageType.Info, "**Filename (without extension): " + withoutExtension);
            
            string directoryName = Path.GetDirectoryName(filename);
            //logger.LogMessage(MessageType.Info, "**Directory Name: " + directoryName);
            
            string contentDirectoryString = "content";
            // int numberOfPathCharactersToCull = directoryName.ToLower().LastIndexOf(contentDirectoryString) + contentDirectoryString.Length + 1;
            
            var numberOfPathCharactersToCull = filename.LastIndexOf("models\\", StringComparison.OrdinalIgnoreCase);
            //logger.LogMessage(MessageType.Info, "**Number of characters to cull: " + numberOfPathCharactersToCull);
            string culledPath = directoryName.Substring(numberOfPathCharactersToCull, directoryName.Length - numberOfPathCharactersToCull); // Used to cull 'content' from path name to create relative pathing.
            //logger.LogMessage(MessageType.Info, "**Culled Path: " + culledPath);

            directoryName.Substring(0, numberOfPathCharactersToCull);
            Path.Combine(directoryName, withoutExtension + ".FBX");
            AssimpContext assimpContext = new AssimpContext();
            assimpContext.SetConfig((PropertyConfig)new NormalSmoothingAngleConfig(66f));
            assimpContext.SetConfig((PropertyConfig)new FBXPreservePivotsConfig(false));
            Scene input = assimpContext.ImportFile(filename,
            	PostProcessSteps.CalculateTangentSpace |
            	PostProcessSteps.JoinIdenticalVertices |
            	PostProcessSteps.Triangulate |
            	PostProcessSteps.GenerateSmoothNormals |
            	PostProcessSteps.SplitLargeMeshes |
            	PostProcessSteps.LimitBoneWeights |
            	PostProcessSteps.SortByPrimitiveType |
            	PostProcessSteps.FindInvalidData |
            	PostProcessSteps.GenerateUVCoords |
            	PostProcessSteps.FlipWindingOrder);

            string outputDir1 = outputDir;
            if (input.MeshCount == 0 && input.AnimationCount == 0)
            {
                throw new Exception("Number of meshes is 0 and no animation present!");
            }
            else
            {
                logger.LogMessage(MessageType.Info, "Found " + input.MeshCount + " meshe(s).", "Meshes");
                logger.LogMessage(MessageType.Info, "Found " + input.AnimationCount + " animation(s).", "Animations");
            }

            if (input.MaterialCount > 0)
            {
                List<MyMaterialConfiguration> materialConfigurationList = new List<MyMaterialConfiguration>();
                for (int index = 0; index < input.MaterialCount; ++index)
                {
                    MyMaterialConfiguration materialConfiguration = getMaterialByRef(input.Materials[index].Name);
                    if (materialConfiguration != null)
                        materialConfigurationList.Add(materialConfiguration);
                }
                if (materialConfigurationList.Count > 0)
                    configuration.Materials = configuration.Materials != null ? ((IEnumerable<MyMaterialConfiguration>)configuration.Materials).Union<MyMaterialConfiguration>((IEnumerable<MyMaterialConfiguration>)materialConfigurationList.ToArray()).ToArray<MyMaterialConfiguration>() : materialConfigurationList.ToArray();
            }

            MyModelProcessor processor = this.CreateProcessor(configuration);
            if (configuration.Materials != null)
            {
                foreach (MyMaterialConfiguration material in configuration.Materials)
                {
                    try
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        if (processor.MaterialProperties.Keys.Contains<string>(material.Name))
                        {
                            logger.LogMessage(MessageType.Warning, "Material: " + material.Name + " is already defined in the processor. Not adding it again..", filename);
                        }
                        else
                        {
                            processor.MaterialProperties.Add(material.Name, dictionary);
                            foreach (MyModelParameter parameter in material.Parameters)
                                dictionary.Add(parameter.Name, (object)parameter.Value);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        logger.LogMessage(MessageType.Warning, "Problem when processing materials: " + ex.Message, filename);
                    }
                }
            }
            int num2 = 999;
            List<MyLODDescriptor> myLodDescriptorList = new List<MyLODDescriptor>();
            for (int index = 0; index < num2; ++index)
            {
                string path = Path.Combine(directoryName, withoutExtension + "_LOD" + (object)(index + 1)) + ".fbx";
                string str2 = Path.Combine(culledPath, withoutExtension + "_LOD" + (object)(index + 1));

                if (File.Exists(path))
                {
                    if (overrideLods && lodDistances != null && (index < lodDistances.Length && (double)lodDistances[index] > 0.0))
                    {
                        MyLODDescriptor myLodDescriptor = new MyLODDescriptor()
                        {
                            Distance = lodDistances[index],
                            Model = str2
                        };
                        myLodDescriptorList.Add(myLodDescriptor);
                    }
                    else if (configuration.LODs != null && index < configuration.LODs.Length)
                    {
                        MyLODConfiguration loD = configuration.LODs[index];
                        MyLODDescriptor myLodDescriptor = new MyLODDescriptor()
                        {
                            Distance = loD.Distance,
                            Model = str2,
                            RenderQuality = loD.RenderQuality
                        };

                        
                        if (str2.ToLower() != loD.Model.ToLower())
                            logger.LogMessage(MessageType.Warning, "LOD" + (object)(index + 1) + " name differs " + str2 + " and " + loD.Model, filename);
                        myLodDescriptorList.Add(myLodDescriptor);
                    }
                    else
                        logger.LogMessage(MessageType.Warning, "LOD" + (object)(index + 1) + " model exists but configuration is missing", filename);
                }
                else if (configuration.LODs != null && index < configuration.LODs.Length)
                    logger.LogMessage(MessageType.Warning, "LOD model " + configuration.LODs[index].Model + " is missing", filename);
                else
                    break;
            }
            processor.LODs = myLodDescriptorList.ToArray();
            processor.BoneGridMapping = configuration.BoneGridSize;
            processor.BoneMapping = configuration.BoneMapping != null ? ((IEnumerable<MyModelVector>)configuration.BoneMapping).Select<MyModelVector, Vector3>((Func<MyModelVector, Vector3>)(s => new Vector3((float)s.X, (float)s.Y, (float)s.Z))).ToArray<Vector3>() : (Vector3[])null;
            processor.HavokCollisionShapes = havokCollisionShapes;
            processor.Process(input, filename, outputDir1, checkOpenBoundaries, logger);
            if (configuration.BoneGridSize.HasValue)
                configuration.BoneMapping = ((IEnumerable<Vector3>)processor.BoneMapping).Select<Vector3, MyModelVector>((Func<Vector3, MyModelVector>)(s => (MyModelVector)s)).ToArray<MyModelVector>();
            List<MyMaterialConfiguration> materialConfigurationList1 = new List<MyMaterialConfiguration>();
            foreach (KeyValuePair<string, Dictionary<string, object>> materialProperty in processor.MaterialProperties)
                materialConfigurationList1.Add(new MyMaterialConfiguration()
                {
                    Name = materialProperty.Key,
                    Parameters = MyModelBuilder.GetParameters(materialProperty)
                });
            configuration.Materials = materialConfigurationList1.Count <= 0 ? (MyMaterialConfiguration[])null : materialConfigurationList1.ToArray();
            if (processor.LODs == null)
                return;
            List<MyLODConfiguration> lodConfigurationList = new List<MyLODConfiguration>();
            foreach (MyLODDescriptor loD in processor.LODs)
                lodConfigurationList.Add(new MyLODConfiguration()
                {
                    Distance = loD.Distance,
                    Model = loD.Model,
                    RenderQuality = loD.RenderQuality
                });
            configuration.LODs = lodConfigurationList.ToArray();
        }

        private MyModelProcessor CreateProcessor(MyModelConfiguration configuration)
        {
            MyModelProcessor myModelProcessor = new MyModelProcessor();
            foreach (MyModelParameter parameter in configuration.Parameters)
            {
                Action<MyModelProcessor, object> action;
                if (this.m_setters.TryGetValue(parameter.Name, out action))
                    action(myModelProcessor, (object)parameter.Value);
            }
            return myModelProcessor;
        }

        private static MyModelParameter[] GetParameters(
          KeyValuePair<string, Dictionary<string, object>> materialProps)
        {
            return materialProps.Value.Select<KeyValuePair<string, object>, MyModelParameter>((Func<KeyValuePair<string, object>, MyModelParameter>)(s => new MyModelParameter()
            {
                Name = s.Key,
                Value = s.Value != null ? s.Value.ToString() : ""
            })).ToArray<MyModelParameter>();
        }
    }
}
