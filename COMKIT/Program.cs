using System;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Linq;
using System.Collections.Generic;

namespace COMKIT
{
    class Program
    {
        static void Main(string[] args)
        {
            var reg = new Registry();


            // ListKeyVal examples
            //sz
            //reg.ListKeyVal(RegKey: "HKEY_LOCAL_MACHINE\\HARDWARE\\DEVICEMAP\\VIDEO", ValueName: "\\Device\\Video7");

            //binary
            //reg.ListKeyVal(RegKey: "HKEY_LOCAL_MACHINE\\HARDWARE\\DEVICEMAP\\VIDEO", ValueName: "ObjectNumberList");

            //dword
            //reg.ListKeyVal(RegKey: "HKEY_LOCAL_MACHINE\\HARDWARE\\DEVICEMAP\\VIDEO", ValueName: "MaxObjectNumber");

            //multi_sz
            //reg.ListKeyVal(RegKey: "HKLM\\SYSTEM\\Setup", ValueName: "CloneTag");

            //show default key
            //reg.ListKeyVal(RegKey: "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{00000319-0000-0000-C000-000000000046}", ValueName: "");

            //CountSubKeys example
            //reg.CountSubKeys(RegKey: "HKEY_LOCAL_MACHINE\\SOFTWARE");

            //ListSubKeys examples
            //reg.ListSubKeys(RegKey: "HKEY_LOCAL_MACHINE\\SOFTWARE\\Node.js");

            //reg.ListSubKeys(RegKey: "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{0000031D-0000-0000-C000-000000000046}");

            //reg.ListSubKeysRecursively(RegKey: "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\ChromeHTML");

            //Write a string example
            reg.WriteRegistryKey(RegKey: "HKEY_CURRENT_USER\\David", RegKeyValue:"Foo", RegKeyDatatype:"SZ", RegKeyData:"Bar");
            Console.WriteLine();


        }


    }
}
