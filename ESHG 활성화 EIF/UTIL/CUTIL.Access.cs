using System.ComponentModel;

using LGCNS.ezControl.Core;

namespace ESHG.EIF.FORM.UTIL
{
    // <summary>
    /// UTIL의 요약
    /// </summary>
    /// 
    public partial class CUTIL
    {
        /// <summary>
        /// Component의 모든 구성요소가 Instancing완료 되었을 때 호출
        /// </summary>
        protected override void OnInstancing()
        {
            base.OnInstancing();
        }

        #region Properties for Variable Access
        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SERVER_CONNECTION
        {
            get
            {
                return this["SERVER_CONNECTION"];
            }
        }

        /// <summary>
        /// Host System Interface Adapter
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ADAPTER_TYPE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ADAPTER_TYPE"];
            }
        }

        /// <summary>
        /// Host Connection Infomation
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_CONNECTION_INFO
        {
            get
            {
                return this["HOST_SERVICE:HOST_CONNECTION_INFO"];
            }
        }

        /// <summary>
        /// Host Message
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_MESSAGE
        {
            get
            {
                return this["HOST_SERVICE:HOST_MESSAGE"];
            }
        }

        /// <summary>
        /// Host Error Code
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_CODE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_CODE"];
            }
        }

        /// <summary>
        /// Host Error Data
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_DATA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_DATA"];
            }
        }

        /// <summary>
        /// Host Error Parameter
        /// </summary>
        [Browsable(false)]
        public CVariable @__HOST_SERVICE__HOST_ERROR_PARA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_PARA"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__CONNECTION_INFO
        {
            get
            {
                return this["SOLACE:CONNECTION_INFO"];
            }
        }

        /// <summary>
        /// Message Log 활성화 여부
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__ENABLE_LOG
        {
            get
            {
                return this["SOLACE:ENABLE_LOG"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_CODE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_CODE"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_MESSAGE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_MESSAGE"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_BIZ
        {
            get
            {
                return this["SOLACE:HOST_ERROR_BIZ"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_DATA
        {
            get
            {
                return this["SOLACE:HOST_ERROR_DATA"];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public CVariable @__SOLACE__HOST_ERROR_LOC
        {
            get
            {
                return this["SOLACE:HOST_ERROR_LOC"];
            }
        }

        /// <summary>
        /// EQP ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_EQP_ID
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_ID"];
            }
        }

        /// <summary>
        /// EIF -> Biz Server Request Queue Name
        /// </summary>
        [Browsable(false)]
        public CVariable @__BIZINFO__V_REQQUEUE_NAME
        {
            get
            {
                return this["BIZINFO:V_REQQUEUE_NAME"];
            }
        }

        /// <summary>
        /// Biz Call TimeOut(mSec)
        /// </summary>
        [Browsable(false)]
        public CVariable @__BIZINFO__V_BIZCALL_TIMEOUT
        {
            get
            {
                return this["BIZINFO:V_BIZCALL_TIMEOUT"];
            }
        }

        /// <summary>
        /// Util Data Scan Interval(Sec) - 0: Not Use
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PC__V_SCAN_INTERVAL
        {
            get
            {
                return this["UTIL_INFO_PC:V_SCAN_INTERVAL"];
            }
        }

        /// <summary>
        /// True - Util Log 남김, False - Log 사용 안함
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PC__V_IS_UTILLOG_USE
        {
            get
            {
                return this["UTIL_INFO_PC:V_IS_UTILLOG_USE"];
            }
        }

        /// <summary>
        /// Util Data Scan Interval(Sec) - 0: Not Use
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PLC__V_SCAN_INTERVAL
        {
            get
            {
                return this["UTIL_INFO_PLC:V_SCAN_INTERVAL"];
            }
        }

        /// <summary>
        /// True - Util Log 남김, False - Log 사용 안함
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PLC__V_IS_UTILLOG_USE
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_UTILLOG_USE"];
            }
        }

        /// <summary>
        /// Power Measure Use Flag : True - USE [O], False - USE [X]
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PLC__V_IS_EM_USE_FLAG
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_EM_USE_FLAG"];
            }
        }

        /// <summary>
        /// Flow Measure Use Flag : True - USE [O], False - USE [X]
        /// </summary>
        [Browsable(false)]
        public CVariable @__UTIL_INFO_PLC__V_IS_FM_USE_FLAG
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_FM_USE_FLAG"];
            }
        }
        #endregion

        #region Properties for Variable Access By Value
        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : List<bool>)
        /// </summary>
        [Browsable(false)]
        public System.Collections.Generic.List<bool> SERVER_CONNECTION
        {
            get
            {
                return this["SERVER_CONNECTION"].AsBooleanList;
            }
            set
            {
                this["SERVER_CONNECTION"].AsBooleanList = value;
            }
        }

        /// <summary>
        /// Host System Interface Adapter
        ///  (Access Type : In/Out, Data Type : )
        /// </summary>
        [Browsable(false)]
        public HOST_ADAPTER_TYPE HOST_SERVICE__HOST_ADAPTER_TYPE
        {
            get
            {
                return (HOST_ADAPTER_TYPE)this["HOST_SERVICE:HOST_ADAPTER_TYPE"].AsEnum;
            }
            set
            {
                this["HOST_SERVICE:HOST_ADAPTER_TYPE"].AsEnum = (int)value;
            }
        }

        /// <summary>
        /// Host Connection Infomation
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_CONNECTION_INFO
        {
            get
            {
                return this["HOST_SERVICE:HOST_CONNECTION_INFO"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_CONNECTION_INFO"].AsString = value;
            }
        }

        /// <summary>
        /// Host Message
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_MESSAGE
        {
            get
            {
                return this["HOST_SERVICE:HOST_MESSAGE"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_MESSAGE"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Code
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_CODE
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_CODE"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_CODE"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Data
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_DATA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_DATA"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// Host Error Parameter
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string HOST_SERVICE__HOST_ERROR_PARA
        {
            get
            {
                return this["HOST_SERVICE:HOST_ERROR_PARA"].AsString;
            }
            set
            {
                this["HOST_SERVICE:HOST_ERROR_PARA"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__CONNECTION_INFO
        {
            get
            {
                return this["SOLACE:CONNECTION_INFO"].AsString;
            }
            set
            {
                this["SOLACE:CONNECTION_INFO"].AsString = value;
            }
        }

        /// <summary>
        /// Message Log 활성화 여부
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool SOLACE__ENABLE_LOG
        {
            get
            {
                return this["SOLACE:ENABLE_LOG"].AsBoolean;
            }
            set
            {
                this["SOLACE:ENABLE_LOG"].AsBoolean = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_CODE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_CODE"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_CODE"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_MESSAGE
        {
            get
            {
                return this["SOLACE:HOST_ERROR_MESSAGE"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_MESSAGE"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_BIZ
        {
            get
            {
                return this["SOLACE:HOST_ERROR_BIZ"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_BIZ"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_DATA
        {
            get
            {
                return this["SOLACE:HOST_ERROR_DATA"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_DATA"].AsString = value;
            }
        }

        /// <summary>
        /// 
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string SOLACE__HOST_ERROR_LOC
        {
            get
            {
                return this["SOLACE:HOST_ERROR_LOC"].AsString;
            }
            set
            {
                this["SOLACE:HOST_ERROR_LOC"].AsString = value;
            }
        }

        /// <summary>
        /// EQP ID
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_INFO__V_W_EQP_ID
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_ID"].AsString;
            }
            set
            {
                this["EQP_INFO:V_W_EQP_ID"].AsString = value;
            }
        }

        /// <summary>
        /// EIF -> Biz Server Request Queue Name
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string BIZINFO__V_REQQUEUE_NAME
        {
            get
            {
                return this["BIZINFO:V_REQQUEUE_NAME"].AsString;
            }
            set
            {
                this["BIZINFO:V_REQQUEUE_NAME"].AsString = value;
            }
        }

        /// <summary>
        /// Biz Call TimeOut(mSec)
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int BIZINFO__V_BIZCALL_TIMEOUT
        {
            get
            {
                return this["BIZINFO:V_BIZCALL_TIMEOUT"].AsInteger;
            }
            set
            {
                this["BIZINFO:V_BIZCALL_TIMEOUT"].AsInteger = value;
            }
        }

        /// <summary>
        /// Util Data Scan Interval(Sec) - 0: Not Use
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int UTIL_INFO_PC__V_SCAN_INTERVAL
        {
            get
            {
                return this["UTIL_INFO_PC:V_SCAN_INTERVAL"].AsInteger;
            }
            set
            {
                this["UTIL_INFO_PC:V_SCAN_INTERVAL"].AsInteger = value;
            }
        }

        /// <summary>
        /// True - Util Log 남김, False - Log 사용 안함
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool UTIL_INFO_PC__V_IS_UTILLOG_USE
        {
            get
            {
                return this["UTIL_INFO_PC:V_IS_UTILLOG_USE"].AsBoolean;
            }
            set
            {
                this["UTIL_INFO_PC:V_IS_UTILLOG_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Util Data Scan Interval(Sec) - 0: Not Use
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int UTIL_INFO_PLC__V_SCAN_INTERVAL
        {
            get
            {
                return this["UTIL_INFO_PLC:V_SCAN_INTERVAL"].AsInteger;
            }
            set
            {
                this["UTIL_INFO_PLC:V_SCAN_INTERVAL"].AsInteger = value;
            }
        }

        /// <summary>
        /// True - Util Log 남김, False - Log 사용 안함
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool UTIL_INFO_PLC__V_IS_UTILLOG_USE
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_UTILLOG_USE"].AsBoolean;
            }
            set
            {
                this["UTIL_INFO_PLC:V_IS_UTILLOG_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Power Measure Use Flag : True - USE [O], False - USE [X]
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool UTIL_INFO_PLC__V_IS_EM_USE_FLAG
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_EM_USE_FLAG"].AsBoolean;
            }
            set
            {
                this["UTIL_INFO_PLC:V_IS_EM_USE_FLAG"].AsBoolean = value;
            }
        }

        /// <summary>
        /// Flow Measure Use Flag : True - USE [O], False - USE [X]
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool UTIL_INFO_PLC__V_IS_FM_USE_FLAG
        {
            get
            {
                return this["UTIL_INFO_PLC:V_IS_FM_USE_FLAG"].AsBoolean;
            }
            set
            {
                this["UTIL_INFO_PLC:V_IS_FM_USE_FLAG"].AsBoolean = value;
            }
        }
        #endregion
    }
}