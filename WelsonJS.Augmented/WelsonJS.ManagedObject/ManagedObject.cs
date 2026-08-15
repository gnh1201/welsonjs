using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("f63b5f28-e278-497b-8525-2601ee7e8c3d")]
    [ProgId("WelsonJS.ManagedObject")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ManagedObject
    {
        public object CreateObject(string progId)
        {
            return CreateObject(progId, null);
        }

        public object CreateObject(string progId, string serverName)
        {
            if (string.Equals(
                progId,
                "htmlfile",
                StringComparison.OrdinalIgnoreCase))
            {
                return CreateHtmlDocument(serverName);
            }

            Type type = Type.GetTypeFromProgID(
                progId,
                serverName,
                true);

            return Activator.CreateInstance(type);
        }

        private object CreateHtmlDocument(string serverName)
        {
            Guid clsid = new Guid(
                "3050F55F-98B5-11CF-BB82-00AA00BDCE0B");

            Type type = Type.GetTypeFromCLSID(
                clsid,
                serverName,
                true);

            return Activator.CreateInstance(type);
        }

        public object[] Invoke(
            object target,
            string method,
            object[] args,
            bool[] byRef)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (string.IsNullOrEmpty(method))
                throw new ArgumentNullException(nameof(method));

            if (args == null)
                args = new object[0];

            if (byRef == null)
                byRef = new bool[args.Length];

            if (byRef.Length != args.Length)
                throw new ArgumentException(
                    "byRef length must match args length.");

            var modifier = new ParameterModifier(args.Length);

            for (int i = 0; i < args.Length; i++)
                modifier[i] = byRef[i];

            object result = target.GetType().InvokeMember(
                method,
                BindingFlags.InvokeMethod |
                BindingFlags.Public |
                BindingFlags.Instance,
                null,
                target,
                args,
                new[] { modifier },
                null,
                null);

            var output = new object[args.Length + 1];

            output[0] = result;

            for (int i = 0; i < args.Length; i++)
                output[i + 1] = args[i];

            return output;
        }
    }
}
