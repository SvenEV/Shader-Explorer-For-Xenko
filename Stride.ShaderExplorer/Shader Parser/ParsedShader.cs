using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public readonly IReadOnlyList<MethodDeclaration> Methods;
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
            Variables = ShaderClass?.Members.OfType<Variable>().ToList() ?? new List<Variable>();
            Methods = ShaderClass?.Members.OfType<MethodDeclaration>().ToList() ?? new List<MethodDeclaration>();
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
