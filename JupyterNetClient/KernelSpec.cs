using System.Collections.Generic;

namespace JupyterNetClient
{
    public class KernelSpec
    {
        public class Definition
        {
            public List<string> argv;
            public string display_name;
            public string language;
            public string interrupt_mode;
            public object env;
            public object metadata;
        }

        public string resource_dir;
        public Definition spec;
    }
}
