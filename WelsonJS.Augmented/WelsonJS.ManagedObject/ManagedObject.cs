// ManagedObject.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("f63b5f28-e278-497b-8525-2601ee7e8c3d")]
    [ProgId("WelsonJS.ManagedObject")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ManagedObject
    {
        private readonly List<object> _createdObjects =
            new List<object>();

        private bool IsManagedObject(object obj)
        {
            if (obj == null)
                return false;

            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (Object.ReferenceEquals(_createdObjects[i], obj))
                    return true;
            }

            return false;
        }

        public object CreateObject(string progId)
        {
            var obj = CreateObject(progId, null);
            _createdObjects.Add(obj);

            return obj;
        }

        public object CreateObject(string progId, string serverName)
        {
            if (string.Equals(progId, "htmlfile", StringComparison.OrdinalIgnoreCase))
            {
                return CreateHtmlDocument(serverName);
            }

            if (string.Equals(progId, "welsonjs.toolkit", StringComparison.OrdinalIgnoreCase))
            {
                progId = "WelsonJS.Legacy.Toolkit";
            }

            Type type = Type.GetTypeFromProgID(progId, serverName, true);

            return Activator.CreateInstance(type);
        }

        private object CreateHtmlDocument(string serverName)
        {
            Guid clsid = new Guid("3050F55F-98B5-11CF-BB82-00AA00BDCE0B");
            Type type = Type.GetTypeFromCLSID(clsid, serverName, true);

            return Activator.CreateInstance(type);
        }

        public object Wrap(object obj)
        {
            if (!IsManagedObject(obj))
                throw new ArgumentException(
                    "The object was not created by ManagedObject.",
                    nameof(obj)
                );

            return new ManagedObjectWrapper(obj);
        }

        public object ByRef()
        {
            return new ByRefArgument();
        }

        public object ByRef(object value)
        {
            return new ByRefArgument
            {
                Value = value
            };
        }
    }
}
