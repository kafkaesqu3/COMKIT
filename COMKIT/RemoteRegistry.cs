using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Security.AccessControl;
using System.ServiceProcess;

namespace COMKIT
{

    class RemoteRegistry
    {
        class Helpers
        {
            //splits full registry key path (HKLM\SOFTWARE\Microsoft) into 2 elements (HKLM and SOFTWARE\Microsoft)
            internal static string[] SplitKey(string key)
            {
                List<string> list = new List<string>(key.Split('\\'));
                var RootKey = list[0];
                list.RemoveAt(0);
                var KeyName = String.Join("\\", list);
                return new string[] { RootKey, KeyName };
            }

            //returns open registry key
            //caller is reponsible for closing this
            internal static RegistryKey GetBaseKey(string RegistryKey, string RemoteHost = "localhost")
            {
                // select correct registry hive from full path
                RegistryHive RegistryHive;
                var rootkey = Helpers.SplitKey(RegistryKey)[0];
                                
                if (rootkey == "HKCU" || rootkey == "HKEY_CURRENT_USER")
                {
                    RegistryHive = RegistryHive.CurrentUser;
                    //String keyName = RegistryKey
                }
                else if (rootkey == "HKLM" || rootkey == "HKEY_LOCAL_MACHINE")
                {
                    RegistryHive = RegistryHive.LocalMachine;
                    //regkey 
                }
                else
                {
                    throw new Exception(string.Format("Root key {0} does not exist", rootkey));
                }

                //select key name from full path
                RegistryKey BaseKey;
                if (RemoteHost == "localhost")
                {
                    BaseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive, RegistryView.Default);
                }
                else
                {
                    BaseKey = Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(RegistryHive, RemoteHost, RegistryView.Default);
                }

                if (BaseKey == null)
                {
                    throw new Exception(string.Format("Error 2"));
                }

                return BaseKey;
            }

            internal static void processValueNamesRecursively(RegistryKey Key)
            { //function to process the valueNames for a given key
                string[] valuenames = Key.GetValueNames();
                if (valuenames == null || valuenames.Length <= 0) //has no values
                    return;
                foreach (string valuename in valuenames)
                {
                    object obj = Key.GetValue(valuename);
                    if (obj != null)
                        PrintKey(Key, valuename);
                }
            }

            internal static void PrintKey(RegistryKey regkey, string ValueName)
            {
                if (regkey.GetValueKind(ValueName).ToString().ToUpper() == "BINARY")
                {
                    byte[] BinData = (byte[])regkey.GetValue(ValueName);
                    string BinString = BitConverter.ToString(BinData).Replace("-", "");
                    if (String.IsNullOrEmpty(ValueName))
                    {
                        Console.WriteLine("{0}\\{1}=(REG_{2}):{3}", regkey, "(Default)", regkey.GetValueKind(ValueName).ToString().ToUpper(), BinString.ToString());
                    }
                    else
                    {
                        Console.WriteLine("{0}]\\{1}=(REG_{2}):{3}", regkey, ValueName, regkey.GetValueKind(ValueName).ToString().ToUpper(), BinString.ToString());
                    }

                }
                else if (regkey.GetValueKind(ValueName).ToString().ToUpper() == "MULTISTRING")
                {
                    Console.WriteLine();
                    string[] tArray = (string[])regkey.GetValue(ValueName);
                    for (int i = 0; i < tArray.Length; i++)
                    {
                        if (String.IsNullOrEmpty(ValueName))
                        {
                            Console.WriteLine("{0}\\{1}=(REG_{2}):{3}", regkey, "(Default)", regkey.GetValueKind(ValueName).ToString().ToUpper(), tArray[i]);
                        }
                        else
                        {
                            Console.WriteLine("{0}\\{1}=(REG_{2}):{3}", regkey, ValueName, regkey.GetValueKind(ValueName).ToString().ToUpper(), tArray[i]);
                        }
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(ValueName))
                    {
                        Console.WriteLine("{0}\\{1}=(REG_{2}):{3}", regkey, "(Default)", regkey.GetValueKind(ValueName).ToString().ToUpper(), regkey.GetValue(ValueName).ToString());
                    }
                    else
                    {
                        Console.WriteLine("{0}\\{1}=(REG_{2}):{3}", regkey, ValueName, regkey.GetValueKind(ValueName).ToString().ToUpper(), regkey.GetValue(ValueName).ToString());
                    }
                }
            }
        }

        
        //Gets the value of a key and prints to console
        public void ListKeyVal(string RegKey, string ValueName = "", string RemoteHost = "localhost")
        {

            var BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];

            RegistryKey key = BaseKey.OpenSubKey(KeyName);
            
            if (key == null)
            {
                throw new Exception(string.Format("Key {0} does not exist", key));
            }
            Helpers.PrintKey(key, ValueName);
            BaseKey.Close();
        }


        //count the number of subkeys in a key
        public int CountSubKeys(string RegKey, string RemoteHost = "localhost")
        {
            
            var BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];
            var key = BaseKey.OpenSubKey(KeyName);

            int count = key.SubKeyCount;
            BaseKey.Close();
            return count;
        }

      

        public void ListSubKeys(string RegKey, string RemoteHost = "localhost")
        {
            var BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];
            var key = BaseKey.OpenSubKey(KeyName);
            //var results = new List<Dictionary<string, string>>();

            foreach (string oVal in key.GetValueNames())
            {
                Helpers.PrintKey(key, oVal);
            }
            Console.WriteLine();
            foreach (string oSubKey in key.GetSubKeyNames())
            {
                Console.WriteLine("{0}\\{1}", RegKey, oSubKey);
            }
            BaseKey.Close();
        }

        public static void GetKeyPerms(string RegKey, string ValueName, string RemoteHost = "localhost")
        {

            var BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];

            RegistrySecurity registrySecurity = BaseKey.GetAccessControl();
            Console.WriteLine("\n{0}\n", BaseKey.Name);
            Console.WriteLine("[*] None:\n{0}\n", registrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.None));
            Console.WriteLine("[*] Audit:\n{0}\n", registrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Audit));
            Console.WriteLine("[*] Access:\n{0}\n", registrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Access));
            Console.WriteLine("[*] Group:\n{0}\n", registrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Group));
            var rules = registrySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
            foreach (var rule in rules.Cast<AuthorizationRule>())
            {
                Console.WriteLine("{0}", rule.IdentityReference.Value);
            }
            BaseKey.Close();
            return;
        }

        public void ListSubKeysRecursively(string RegKey, string RemoteHost = "localhost")
        {
            RegistryKey BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];
            var Key = BaseKey.OpenSubKey(KeyName);
            try
            {
                string[] subkeynames = Key.GetSubKeyNames(); //means deeper folder
                if (subkeynames == null || subkeynames.Length <= 0)
                { //has no more subkey, process
                    //PrintKey(Key, "this shouldnt happen?");
                    Helpers.processValueNamesRecursively(Key);
                    return;
                }
                foreach (string keyname in subkeynames) { //has subkeys, go deeper
                    using (RegistryKey key2 = Key.OpenSubKey(keyname))
                    {
                        ListSubKeysRecursively(key2.Name, RemoteHost);
                    }                        
                }
                Helpers.processValueNamesRecursively(Key);
            }
            catch (Exception e)
            {
                BaseKey.Close();
                throw new Exception(string.Format("RecursiveOutput error {0}", e));
            }
            BaseKey.Close();
        }

        public void WriteRegistryKey(string RegKey, string RegKeyValue, string RegKeyDatatype, string RegKeyData, string RemoteHost="localhost")
        {
            string[] supportedDataTypes = { "SZ", "EXPAND_SZ", "MULTI_SZ", "DWORD", "QWORD", "BINARY"};
            if (!supportedDataTypes.Contains(RegKeyDatatype))
            {
                throw new Exception(string.Format("Invalid data type specified: {0}", RegKeyDatatype));
            }
            RegistryKey BaseKey = Helpers.GetBaseKey(RegKey, RemoteHost);
            var KeyName = Helpers.SplitKey(RegKey)[1];
            //var Key = BaseKey.OpenSubKey(KeyName);

            try
            {
                RegistryKey NewKey = BaseKey.CreateSubKey(KeyName);
                if (RegKeyDatatype.ToUpper() == "SZ")
                {
                    NewKey.SetValue(RegKeyValue, RegKeyData, RegistryValueKind.String);
                    Console.WriteLine("\nThe add opetation of {0} was successful.", RegKey);
                    BaseKey.Close();
                    return;
                }
                else if (RegKeyDatatype.ToUpper() == "EXPAND_SZ")
                {
                    NewKey.SetValue(RegKeyValue, RegKeyData, RegistryValueKind.ExpandString);
                    Console.WriteLine("\nThe add operation of {0} was successful.", RegKey);
                    BaseKey.Close();
                    return;
                }
                else if (RegKeyDatatype.ToUpper() == "MULTI_SZ")
                {
                    //NewKey.SetValue(ValueName, ValueData, RegistryValueKind.MultiString);
                    //Console.WriteLine("\nThe add opetation of {0} was successful.", KeyName);
                    Console.WriteLine("\nMulti-String feature is not implemented yet.");
                    BaseKey.Close();
                    return;
                }
                else if (RegKeyDatatype.ToUpper() == "DWORD")
                {
                    NewKey.SetValue(RegKeyValue, int.Parse(RegKeyData), RegistryValueKind.DWord);
                    Console.WriteLine("\nThe add operation of {0} was successful.", RegKey);
                    BaseKey.Close();
                    return;
                }
                else if (RegKeyDatatype.ToUpper() == "QWORD")
                {
                    NewKey.SetValue(RegKeyValue, int.Parse(RegKeyData), RegistryValueKind.QWord);
                    Console.WriteLine("\nThe add operation of {0} was successful.", RegKey);
                    BaseKey.Close();
                    return;
                }
                else if (RegKeyDatatype.ToUpper() == "BINARY")
                {
                    byte[] ValueByte = System.Text.Encoding.ASCII.GetBytes(RegKeyData);
                    NewKey.SetValue(RegKeyValue, ValueByte, RegistryValueKind.Binary);
                    Console.WriteLine("\nThe add operation of {0} was successful.", RegKey);
                    BaseKey.Close();
                    return;
                }
                else
                {
                    throw new Exception(string.Format("Invalid data type specified: {0}", RegKeyDatatype));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n [!] {0}: {1}", e.GetType().Name, e.Message);
                return;
            }
        }

        
    }
}
