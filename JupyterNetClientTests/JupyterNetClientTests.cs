using System;
using JupyterNetClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JupyterNetClientTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CanCreate()
        {
            var client = new JupyterClient();
            var kernels = client.GetKernels();
        }
    }
}
