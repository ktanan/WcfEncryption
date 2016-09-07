using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Web.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;

namespace WcfEncryption
{

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service.svc or Service.svc.cs at the Solution Explorer and start debugging.
    public class Service : IService
    {

        public sealed class UnmanagedLibrary : IDisposable

        {

            #region Safe Handles and Native imports

            // See http://msdn.microsoft.com/msdnmag/issues/05/10/Reliability/ for more about safe handles.

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]

            sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid

            {

                private SafeLibraryHandle() : base(true) { }

                protected override bool ReleaseHandle()

                {

                    return NativeMethods.FreeLibrary(handle);

                }

            }


            static class NativeMethods

            {

                const string s_kernel = "kernel32";

                [DllImport(s_kernel, CharSet = CharSet.Ansi, BestFitMapping = false, SetLastError = true)]
                // [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
                public static extern SafeLibraryHandle LoadLibrary(string fileName);



                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]

                [DllImport(s_kernel, SetLastError = true)]

                [return: MarshalAs(UnmanagedType.Bool)]

                public static extern bool FreeLibrary(IntPtr hModule);

                //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                [DllImport(s_kernel)]
                // [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
                public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, String procname);

            }

            #endregion // Safe Handles and Native imports


            /// <summary>

            /// Constructor to load a dll and be responible for freeing it.

            /// </summary>

            /// <param name=”fileName”>full path name of dll to load</param>

            /// <exception cref=”System.IO.FileNotFound”>if fileName can’t be found</exception>

            /// <remarks>Throws exceptions on failure. Most common failure would be file-not-found, or

            /// that the file is not a  loadable image.</remarks>

            public UnmanagedLibrary(string fileName)

            {

                m_hLibrary = NativeMethods.LoadLibrary(fileName);

                if (m_hLibrary.IsInvalid)

                {

                    int hr = Marshal.GetHRForLastWin32Error();

                    Marshal.ThrowExceptionForHR(hr);

                }

            }


            /// <summary>

            /// Dynamically lookup a function in the dll via kernel32!GetProcAddress.

            /// </summary>

            /// <param name=”functionName”>raw name of the function in the export table.</param>

            /// <returns>null if function is not found. Else a delegate to the unmanaged function.

            /// </returns>

            /// <remarks>GetProcAddress results are valid as long as the dll is not yet unloaded. This

            /// is very very dangerous to use since you need to ensure that the dll is not unloaded

            /// until after you’re done with any objects implemented by the dll. For example, if you

            /// get a delegate that then gets an IUnknown implemented by this dll,

            /// you can not dispose this library until that IUnknown is collected. Else, you may free

            /// the library and then the CLR may call release on that IUnknown and it will crash.</remarks>

            public TDelegate GetUnmanagedFunction<TDelegate>(string functionName) where TDelegate : class

            {

                IntPtr p = NativeMethods.GetProcAddress(m_hLibrary, functionName);


                // Failure is a common case, especially for adaptive code.

                if (p == IntPtr.Zero)

                {

                    return null;

                }

                Delegate function = Marshal.GetDelegateForFunctionPointer(p, typeof(TDelegate));


                // Ideally, we’d just make the constraint on TDelegate be

                // System.Delegate, but compiler error CS0702 (constrained can’t be System.Delegate)

                // prevents that. So we make the constraint system.object and do the cast from object–>TDelegate.

                object o = function;


                return (TDelegate)o;

            }


            #region IDisposable Members

            /// <summary>

            /// Call FreeLibrary on the unmanaged dll. All function pointers

            /// handed out from this class become invalid after this.

            /// </summary>

            /// <remarks>This is very dangerous because it suddenly invalidate

            /// everything retrieved from this dll. This includes any functions

            /// handed out via GetProcAddress, and potentially any objects returned

            /// from those functions (which may have an implemention in the

            /// dll).

            /// </remarks>

            public void Dispose()
            {
                if (!m_hLibrary.IsClosed)
                {
                    m_hLibrary.Close();
                }
            }


            // Unmanaged resource. CLR will ensure SafeHandles get freed, without requiring a finalizer on this class.

            SafeLibraryHandle m_hLibrary;
            #endregion

        } // UnmanagedLibrary




        public void DoWork()
        {
        }

        private Wcf_Response_PinEncrypt m_OutputInfo;
        private string EventLog = "";
        public Wcf_Response_PinEncrypt GetInfoResponse
        {
            get { return m_OutputInfo; }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U4)]
        public delegate Int32 _pinPack(String strPin, String strPan, IntPtr strRet);


        public Wcf_Response_PinEncrypt PinEncrypt(string PIPIN,string PIRLCRD)
        {
            string strRet = "";
            try
            {

                #region 'commnet'
                //m_OutputInfo.POPINPACK = String.Format("XXX{0}", PIPIN);
                //byte[] byteArray = Encoding.UTF8.GetBytes(PIPIN.ToString());
                //Byte[] newByteArray;
                //using (MemoryStream plainText = new MemoryStream(byteArray))
                //{
                //    using (MemoryStream encryptedData = new MemoryStream())
                //    {
                //        SharpAESCrypt.SharpAESCrypt.Encrypt(PIPIN.ToString(), plainText, encryptedData);
                //        newByteArray = encryptedData.ToArray();
                //    }
                //}
                //m_OutputInfo.POPINPACK = Convert.ToBase64String(newByteArray);
                #endregion

                string str = String.Empty;
                Int32 ierr = 0;
                String encPIN = String.Empty;

                var handleRet = GCHandle.Alloc(str, GCHandleType.Pinned);
                //using (UnmanagedLibrary lib = new UnmanagedLibrary(@"D:\DES_Pinpack.dll"))
                using (UnmanagedLibrary lib = new UnmanagedLibrary(WebConfigurationManager.AppSettings["DLL"].ToString().ToUpper().Trim()))
                {
                    //IntPtr pRet = handleRet.AddrOfPinnedObject();
                    _pinPack pinPack = lib.GetUnmanagedFunction<_pinPack>("_pinPack");
                    ierr = pinPack(PIPIN, PIRLCRD, handleRet.AddrOfPinnedObject());
                    strRet = Marshal.PtrToStringAnsi(handleRet.AddrOfPinnedObject());

                    handleRet.Free();
                    //handleRet.Free();

                } // implicit call to lib.Dispose, which calls FreeLibrary.


                //remaining code here

                m_OutputInfo.POMCHKEY = "0";
                m_OutputInfo.POPINPACK = "0";
                m_OutputInfo.POMSG = "PinPack is null";

                if (!String.IsNullOrEmpty(strRet))
                {
                    if (strRet.Length >= 21)
                    {
                        //string strMkey = strRet.Substring(0, 5);
                        //ring strPin = strRet.Substring(5, 16);
                        string[] strArr = strRet.Split(',');

                        m_OutputInfo.POMCHKEY = strArr[0];
                        m_OutputInfo.POPINPACK = strArr[1].Substring(0, 16);
                        m_OutputInfo.POMSG = "OK";

                        encPIN = strArr[1].Substring(0, 16);
                        if (WebConfigurationManager.AppSettings["HidePinPack"].ToString().ToUpper().Trim() == "1")
                        {
                            encPIN = m_OutputInfo.POPINPACK.Replace(m_OutputInfo.POPINPACK.Substring(m_OutputInfo.POPINPACK.Length - 5, 5), "XXXXX");
                        }
                    }
                }

                if (WebConfigurationManager.AppSettings["HidePinIn"].ToString().ToUpper().Trim() == "1")
                {
                    PIPIN = "XXXX";
                }

                EventLog = String.Format("Pin in:{0} || RL Card:{1} || Machine key: {2} || Pin pack: {3}|| Message:{4}", PIPIN,PIRLCRD, m_OutputInfo.POMCHKEY, encPIN, m_OutputInfo.POMSG);
                if (WebConfigurationManager.AppSettings["WriteEventLog"].ToString().ToUpper().Trim() == "TRUE")
                {
                    Log.Logger.LogFilePath = WebConfigurationManager.AppSettings["EventLogPathFile"].ToString().Trim();
                    Log.Logger.WriteTrace(false, EventLog);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.LogFilePath = WebConfigurationManager.AppSettings["PathFileName"].ToString().Trim();
                Log.Logger.ErrorRoutine(false, ex);
                m_OutputInfo.POMCHKEY = "0";
                m_OutputInfo.POPINPACK = "0";
                m_OutputInfo.POMSG = ex.Message;
            }
            return this.GetInfoResponse;
        }


    }


}
