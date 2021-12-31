using Microsoft.Toolkit.Mvvm.ComponentModel;
using Stride.Core.Shaders.Ast;
using Stride.ShaderParser;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace StrideShaderExplorer
{
    public class MemberViewModel : ObservableObject
    {
        public MemberViewModel(string name, IDeclaration member)
        {
            Name = name;
            Member = member;
        }

        public List<ShaderViewModel> DeclaringShaders { get; } = new List<ShaderViewModel>();

        public List<ShaderViewModel> UsingShaders { get; } = new List<ShaderViewModel>();

        public string Name { get; }

        public bool IsVariable => Member is Variable;

        public bool IsMethod => Member is MethodDeclaration;

        public IDeclaration Member { get; }

        public override string ToString()
        {
            return Member.ToString();
        }
    }
}
