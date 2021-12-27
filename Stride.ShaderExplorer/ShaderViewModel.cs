using GalaSoft.MvvmLight;
using Stride.ShaderParser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StrideShaderExplorer
{
    public class ShaderViewModel : ObservableObject
    {
        private bool _isExpanded = false;
        private bool _isVisible = true;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { Set(ref _isVisible, value); }
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public ParsedShader ParsedShader { get; set; }

        public List<ShaderViewModel> DerivedShaders { get; } = new List<ShaderViewModel>();

        public List<ShaderViewModel> BaseShaders { get; } = new List<ShaderViewModel>();

        public override string ToString() => ToString(0);

        public string ToString(int depth)
        {
            return Name + string.Join("", BaseShaders
                .Select(o => "\r\n" + "".PadLeft(depth * 4) + o.ToString(depth + 1)));
        }
    }
}
