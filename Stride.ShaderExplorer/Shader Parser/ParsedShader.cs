using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.ShaderParser
{
    public class ParsedShader
    {
        public readonly Shader Shader;
        public readonly ClassType ShaderClass;

        // base shaders
        public IReadOnlyList<ParsedShader> BaseShaders => baseShaders;
        private readonly List<ParsedShader> baseShaders = new List<ParsedShader>();

        // compositions
        public IReadOnlyDictionary<string, CompositionInput> Compositions => compositions;
        private readonly Dictionary<string, CompositionInput> compositions;

        public IReadOnlyDictionary<string, CompositionInput> CompositionsWithBaseShaders => compositionsWithBaseShaders.Value;

        Lazy<IReadOnlyDictionary<string, CompositionInput>> compositionsWithBaseShaders;

        public readonly IReadOnlyList<Variable> Variables;
        public readonly IReadOnlyDictionary<string, Variable> VariablesByName;

        private IEnumerable<CompositionInput> GetCompositionsWithBaseShaders()
        {
            foreach (var comp in Compositions)
            {
                yield return comp.Value;
            }

            foreach (var baseClass in BaseShaders)
            {
                foreach (var baseComp in baseClass.Compositions)
                {
                    yield return baseComp.Value;
                }
            }
        }

        public ParsedShader(Shader shader)
        {
            Shader = shader;
            ShaderClass = Shader.GetFirstClassDecl();
            Variables = ShaderClass?.Members.OfType<Variable>().Where(v => !v.Qualifiers.Contains(StrideStorageQualifier.Stream)).ToList() ?? new List<Variable>(); //should include parent shaders?
            VariablesByName = Variables.ToDictionary(v => v.Name.Text);
            compositions = Variables
                .Select((v, i) => (v, i))
                .Where(v => v.v.Qualifiers.Contains(StrideStorageQualifier.Compose))
                .Select(v => new CompositionInput(v.v, v.i))
                .ToDictionary(v => v.Name);

            compositionsWithBaseShaders = new Lazy<IReadOnlyDictionary<string, CompositionInput>>(() => GetCompositionsWithBaseShaders().ToDictionary(c => c.Name));
        }

        public void AddBaseShader(ParsedShader baseShader)
        {
            if (!baseShaders.Contains(baseShader))
                baseShaders.Add(baseShader);

        }

        public IEnumerable<ParameterKey> GetUniformInputs()
        {
            foreach (var v in Variables)
            {
                var type = v.Type;
                var keyName = ShaderClass.Name + "." + v.Name;

                switch (type)
                {
                    case ScalarType s when s.Name.Text == "float":
                        yield return ParameterKeys.NewValue(v.GetDefault<float>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "int":
                        yield return ParameterKeys.NewValue(v.GetDefault<int>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "uint":
                        yield return ParameterKeys.NewValue(v.GetDefault<uint>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "bool":
                        yield return ParameterKeys.NewValue(v.GetDefault<bool>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float2":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector2>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float3":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector3>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float4":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector4>(), keyName);
                        break;
                    case TypeName m when m.Name.Text == "float4x4":
                        yield return ParameterKeys.NewValue(Matrix.Identity, keyName);
                        break;
                    case TypeName s when s.Name.Text == "int2":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int2>(), keyName);
                        break;
                    case TypeName s when s.Name.Text == "int3":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int3>(), keyName);
                        break;
                    case TypeName s when s.Name.Text == "int4":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int4>(), keyName);
                        break;
                    case TextureType t:
                        yield return new ObjectParameterKey<Texture>(keyName);
                        break;
                    case ObjectType o when o.Name.Text == "SamplerState":
                        yield return new ObjectParameterKey<SamplerState>(keyName);
                        break;
                    case GenericType b when b.Name.Text.Contains("Buffer"):
                        yield return new ObjectParameterKey<Buffer>(keyName);
                        break;
                    case GenericType t when t.Name.Text.Contains("Texture"):
                        yield return new ObjectParameterKey<Texture>(keyName);
                        break;
                    default:
                        break;
                }
            }
        }

        public override string ToString()
        {
            return ShaderClass?.ToString() ?? base.ToString();
        }
    }

    public class ParsedShaderRef
    {
        public ParsedShader ParsedShader;
        public Stack<ParsedShader> ParentShaders = new Stack<ParsedShader>();
    }

    public class UniformInput
    {
        public string Name;
        public Type Type;

    }

    public class CompositionInput
    {
        public readonly string Name;
        public readonly string TypeName;
        public readonly string Summary;
        public readonly string Remarks;
        public bool IsOptional;
        public readonly PermutationParameterKey<ShaderSource> Key;

        /// <summary>
        /// The local index of this variable in the shader file.
        /// </summary>
        public readonly int LocalIndex;

        public readonly Variable Variable;

        public CompositionInput(Variable v, int localIndex)
        {
            Name = v.Name.Text;
            TypeName = v.Type.Name.Text;
            Key = new PermutationParameterKey<ShaderSource>(Name);
            LocalIndex = localIndex;
            Variable = v;
        }
    }
}
