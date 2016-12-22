using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace XenkoShaderExplorer
{
    public class Shader : ObservableObject
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

        public List<Shader> DerivedShaders { get; } = new List<Shader>();

        public List<Shader> BaseShaders { get; } = new List<Shader>();

        public override string ToString() => ToString(0);

        public string ToString(int depth)
        {
            return Name + string.Join("", BaseShaders
                .Select(o => "\r\n" + "".PadLeft(depth * 4) + o.ToString(depth + 1)));
        }
    }
}
