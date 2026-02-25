using System.Collections.Generic;
using System.ComponentModel;

using LGCNS.ezControl.Core;
using ESHG.EIF.FORM.EOL;

namespace ESHG.EIF.FORM.EOL
{


    /// <summary>
    /// CEOL_BIZ의 요약
    /// </summary>
    public partial class CEOL_BIZ
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
        /// EQP LANE ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_LANE_ID
        {
            get
            {
                return this["EQP_INFO:V_W_LANE_ID"];
            }
        }

        /// <summary>
        /// EQP KIND CD
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_EQP_KIND_CD
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_KIND_CD"];
            }
        }

        /// <summary>
        /// PRINTER_USE
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_PRINTER_USE
        {
            get
            {
                return this["EQP_INFO:V_W_PRINTER_USE"];
            }
        }

        /// <summary>
        /// LOW VOLT USE
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_W_LOW_VOLT_USE
        {
            get
            {
                return this["EQP_INFO:V_W_LOW_VOLT_USE"];
            }
        }

        /// <summary>
        /// True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용
        /// </summary>
        [Browsable(false)]
        public CVariable @__EQP_INFO__V_IS_SIXLOSSCODE_USE
        {
            get
            {
                return this["EQP_INFO:V_IS_SIXLOSSCODE_USE"];
            }
        }

        /// <summary>
        /// EIF -> Biz Server Req Queue Name
        /// </summary>
        [Browsable(false)]
        public CVariable @__BIZ_INFO__V_REQQUEUE_NAME
        {
            get
            {
                return this["BIZ_INFO:V_REQQUEUE_NAME"];
            }
        }

        /// <summary>
        /// Biz Call TimeOut(mSec)
        /// </summary>
        [Browsable(false)]
        public CVariable @__BIZ_INFO__V_BIZCALL_TIMEOUT
        {
            get
            {
                return this["BIZ_INFO:V_BIZCALL_TIMEOUT"];
            }
        }

        /// <summary>
        /// 설비 공장명
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_FACTORY
        {
            get
            {
                return this["MONITOR:V_MONITOR_FACTORY"];
            }
        }

        /// <summary>
        /// 설비 카테고리
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_CATEGORY
        {
            get
            {
                return this["MONITOR:V_MONITOR_CATEGORY"];
            }
        }

        /// <summary>
        /// 설비 ID
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_EQUIPMENT_ID
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQUIPMENT_ID"];
            }
        }

        /// <summary>
        /// 장비 NIC Name
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_EQP_NICNAME
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQP_NICNAME"];
            }
        }

        /// <summary>
        /// 설비 장비타입
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_DEVICETYPE
        {
            get
            {
                return this["MONITOR:V_MONITOR_DEVICETYPE"];
            }
        }

        /// <summary>
        /// Host와의 통신 상태 Online,Offline
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE
        {
            get
            {
                return this["MONITOR:V_MONITOR_HOST_COMMUNICATIONSTATE"];
            }
        }

        /// <summary>
        /// PLC와의 통신 상태
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_PLC_COMMNUICATION
        {
            get
            {
                return this["MONITOR:V_MONITOR_PLC_COMMNUICATION"];
            }
        }

        /// <summary>
        /// CIM Online Status 상태 Auto,Pausing,Paused..Reconcileing
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_CIMSTATUS
        {
            get
            {
                return this["MONITOR:V_MONITOR_CIMSTATUS"];
            }
        }

        /// <summary>
        /// Biz Version
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_BIZ_VERSION
        {
            get
            {
                return this["MONITOR:V_MONITOR_BIZ_VERSION"];
            }
        }

        /// <summary>
        /// Scan Interval
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_SCAN_INTERVAL
        {
            get
            {
                return this["MONITOR:V_MONITOR_SCAN_INTERVAL"];
            }
        }

        /// <summary>
        /// MCCS 운영 기준 ENG01 또는 ENG02 의 Base HostName. 어떤 Node에서 운영중인지 확인 용도
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_BASE_HOSTNAME
        {
            get
            {
                return this["MONITOR:V_MONITOR_BASE_HOSTNAME"];
            }
        }

        /// <summary>
        /// 통합관리로 Risk 정보를 Notification
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_NOTIFICATION
        {
            get
            {
                return this["MONITOR:V_MONITOR_NOTIFICATION"];
            }
        }

        /// <summary>
        /// MCS HSMS driver의 Local Host IP (virtual IP)
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_LOCAL_HOST_IP
        {
            get
            {
                return this["MONITOR:V_MONITOR_LOCAL_HOST_IP"];
            }
        }

        /// <summary>
        /// Solace 접속 상태
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_SOLACE
        {
            get
            {
                return this["MONITOR:V_MONITOR_SOLACE"];
            }
        }

        /// <summary>
        /// Factova Version
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_FACTOVA_VER
        {
            get
            {
                return this["MONITOR:V_MONITOR_FACTOVA_VER"];
            }
        }

        /// <summary>
        /// EQP Status Run, Wait, Trouble, User Stop
        /// </summary>
        [Browsable(false)]
        public CVariable @__MONITOR__V_MONITOR_EQPSTATUS
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQPSTATUS"];
            }
        }
        #endregion

        #region Properties for Variable Access By Value
        /// <summary>
        /// EQP LANE ID
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_INFO__V_W_LANE_ID
        {
            get
            {
                return this["EQP_INFO:V_W_LANE_ID"].AsString;
            }
            set
            {
                this["EQP_INFO:V_W_LANE_ID"].AsString = value;
            }
        }

        /// <summary>
        /// EQP KIND CD
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_INFO__V_W_EQP_KIND_CD
        {
            get
            {
                return this["EQP_INFO:V_W_EQP_KIND_CD"].AsString;
            }
            set
            {
                this["EQP_INFO:V_W_EQP_KIND_CD"].AsString = value;
            }
        }

        /// <summary>
        /// PRINTER_USE
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_INFO__V_W_PRINTER_USE
        {
            get
            {
                return this["EQP_INFO:V_W_PRINTER_USE"].AsBoolean;
            }
            set
            {
                this["EQP_INFO:V_W_PRINTER_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// LOW VOLT USE
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string EQP_INFO__V_W_LOW_VOLT_USE
        {
            get
            {
                return this["EQP_INFO:V_W_LOW_VOLT_USE"].AsString;
            }
            set
            {
                this["EQP_INFO:V_W_LOW_VOLT_USE"].AsString = value;
            }
        }

        /// <summary>
        /// True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용
        ///  (Access Type : In/Out, Data Type : bool)
        /// </summary>
        [Browsable(false)]
        public bool EQP_INFO__V_IS_SIXLOSSCODE_USE
        {
            get
            {
                return this["EQP_INFO:V_IS_SIXLOSSCODE_USE"].AsBoolean;
            }
            set
            {
                this["EQP_INFO:V_IS_SIXLOSSCODE_USE"].AsBoolean = value;
            }
        }

        /// <summary>
        /// EIF -> Biz Server Req Queue Name
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string BIZ_INFO__V_REQQUEUE_NAME
        {
            get
            {
                return this["BIZ_INFO:V_REQQUEUE_NAME"].AsString;
            }
            set
            {
                this["BIZ_INFO:V_REQQUEUE_NAME"].AsString = value;
            }
        }

        /// <summary>
        /// Biz Call TimeOut(mSec)
        ///  (Access Type : In/Out, Data Type : int)
        /// </summary>
        [Browsable(false)]
        public int BIZ_INFO__V_BIZCALL_TIMEOUT
        {
            get
            {
                return this["BIZ_INFO:V_BIZCALL_TIMEOUT"].AsInteger;
            }
            set
            {
                this["BIZ_INFO:V_BIZCALL_TIMEOUT"].AsInteger = value;
            }
        }

        /// <summary>
        /// 설비 공장명
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_FACTORY
        {
            get
            {
                return this["MONITOR:V_MONITOR_FACTORY"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_FACTORY"].AsString = value;
            }
        }

        /// <summary>
        /// 설비 카테고리
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_CATEGORY
        {
            get
            {
                return this["MONITOR:V_MONITOR_CATEGORY"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_CATEGORY"].AsString = value;
            }
        }

        /// <summary>
        /// 설비 ID
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_EQUIPMENT_ID
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQUIPMENT_ID"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_EQUIPMENT_ID"].AsString = value;
            }
        }

        /// <summary>
        /// 장비 NIC Name
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_EQP_NICNAME
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQP_NICNAME"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_EQP_NICNAME"].AsString = value;
            }
        }

        /// <summary>
        /// 설비 장비타입
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_DEVICETYPE
        {
            get
            {
                return this["MONITOR:V_MONITOR_DEVICETYPE"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_DEVICETYPE"].AsString = value;
            }
        }

        /// <summary>
        /// Host와의 통신 상태 Online,Offline
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE
        {
            get
            {
                return this["MONITOR:V_MONITOR_HOST_COMMUNICATIONSTATE"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_HOST_COMMUNICATIONSTATE"].AsString = value;
            }
        }

        /// <summary>
        /// PLC와의 통신 상태
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_PLC_COMMNUICATION
        {
            get
            {
                return this["MONITOR:V_MONITOR_PLC_COMMNUICATION"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_PLC_COMMNUICATION"].AsString = value;
            }
        }

        /// <summary>
        /// CIM Online Status 상태 Auto,Pausing,Paused..Reconcileing
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_CIMSTATUS
        {
            get
            {
                return this["MONITOR:V_MONITOR_CIMSTATUS"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_CIMSTATUS"].AsString = value;
            }
        }

        /// <summary>
        /// Biz Version
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_BIZ_VERSION
        {
            get
            {
                return this["MONITOR:V_MONITOR_BIZ_VERSION"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_BIZ_VERSION"].AsString = value;
            }
        }

        /// <summary>
        /// Scan Interval
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_SCAN_INTERVAL
        {
            get
            {
                return this["MONITOR:V_MONITOR_SCAN_INTERVAL"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_SCAN_INTERVAL"].AsString = value;
            }
        }

        /// <summary>
        /// MCCS 운영 기준 ENG01 또는 ENG02 의 Base HostName. 어떤 Node에서 운영중인지 확인 용도
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_BASE_HOSTNAME
        {
            get
            {
                return this["MONITOR:V_MONITOR_BASE_HOSTNAME"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_BASE_HOSTNAME"].AsString = value;
            }
        }

        /// <summary>
        /// 통합관리로 Risk 정보를 Notification
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_NOTIFICATION
        {
            get
            {
                return this["MONITOR:V_MONITOR_NOTIFICATION"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_NOTIFICATION"].AsString = value;
            }
        }

        /// <summary>
        /// MCS HSMS driver의 Local Host IP (virtual IP)
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_LOCAL_HOST_IP
        {
            get
            {
                return this["MONITOR:V_MONITOR_LOCAL_HOST_IP"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_LOCAL_HOST_IP"].AsString = value;
            }
        }

        /// <summary>
        /// Solace 접속 상태
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_SOLACE
        {
            get
            {
                return this["MONITOR:V_MONITOR_SOLACE"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_SOLACE"].AsString = value;
            }
        }

        /// <summary>
        /// Factova Version
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_FACTOVA_VER
        {
            get
            {
                return this["MONITOR:V_MONITOR_FACTOVA_VER"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_FACTOVA_VER"].AsString = value;
            }
        }

        /// <summary>
        /// EQP Status Run, Wait, Trouble, User Stop
        ///  (Access Type : In/Out, Data Type : string)
        /// </summary>
        [Browsable(false)]
        public string MONITOR__V_MONITOR_EQPSTATUS
        {
            get
            {
                return this["MONITOR:V_MONITOR_EQPSTATUS"].AsString;
            }
            set
            {
                this["MONITOR:V_MONITOR_EQPSTATUS"].AsString = value;
            }
        }
        #endregion
    }
}