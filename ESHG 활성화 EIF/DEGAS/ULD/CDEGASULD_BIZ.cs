using System;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;
using LGCNS.ezControl.EIF.Solace;

using SolaceSystems.Solclient.Messaging;
using Newtonsoft.Json;

using ESHG.EIF.FORM.COMMON;
using System.Reflection;

namespace ESHG.EIF.FORM.DEGASULD
{
    public partial class CDEGASULD_BIZ : CImplement, IEIF_Biz
    {
        #region Class Member variable
        public static string EQPTYPE = "DegasUnloader";

        public short SCANINTERVAL = 500; //msec
        public short SECINTERVAL = 10;    //sec

        #region Simulation Mode 설정 관련
        public const Boolean SIMULATION_MODE = false; //$ 2021.07.12 : 사전 검수 모드는 빌드를 통해서만 바꿀수 있게 하자
        public bool IsRl { get { return !SIMULATION_MODE; } } //IsReal을 FullName로 쓰면 너무길어서 약어로 쓴다. 알아봐야 할텐데..
        #endregion

        #region Host Simulation Mode 설정 관련
        public const Boolean HOST_SIMULATION_MODE = false; //$ 2021.11.24 : GMES 통합 테스트 모드는 빌드를 통해서만 바꿀수 있게 하자
        public bool IsHR { get { return !HOST_SIMULATION_MODE; } } //IsHostReal을 FullName로 쓰면 너무길어서 약어로 쓴다. 알아봐야 할텐데..        
        #endregion

        //$ 2024.11.22 : Solace Request/Reply Queue 변수 추가
        private string ReqQueue { get { return this.BIZ_INFO__V_REQQUEUE_NAME; } }
        private string RepQueue { get { return $"REPLY/{this.BIZ_INFO__V_REQQUEUE_NAME}"; } }

        public int BizTimeout { get { return this.BIZ_INFO__V_BIZCALL_TIMEOUT; } }

        public string EifFileName => $"{this.Name}{"_EIF"}";

        private CDEGASULD BASE { get { return (Owner as CDEGASULD); } }

        public string EQPID { get { return this.BASE.EQP_INFO__V_W_EQP_ID; } }
        public string SEALEREQPID { get { return this.BASE.EQP_INFO__V_W_SUBEQP_ID_01; } }
        public string HOTPRESSEQPID { get { return this.BASE.EQP_INFO__V_W_SUBEQP_ID_02; } }
        public string DEGASEQPID { get { return this.BASE.EQP_INFO__V_W_EXTEQP_ID; } }

        private ushort PreSubState { get; set; } //$ 2023.05.18 : EQPState가 8로 변경 될 때 SubState 저장, 나머지 상태에서는 0으로 입력 됨
        private ushort PreSealerSubState { get; set; } //$ 2023.05.18 : EQPState가 8로 변경 될 때 SubState 저장, 나머지 상태에서는 0으로 입력 됨
        private ushort PreHotPressSubState { get; set; } //$ 2023.05.18 : EQPState가 8로 변경 될 때 SubState 저장, 나머지 상태에서는 0으로 입력 됨

        private string[] _arrTactEQPID = null;
        public string[] TactEQPIDs
        {
            get
            {
                if (_arrTactEQPID == null)
                {
                    #region IO Variable에서 TactTime 보고 대상 추출
                    _arrTactEQPID = new string[1];
                    this.TactEQPIDs.SetValue(this.BASE.EQP_INFO__V_W_EQP_ID, 0);

                    int subUnitCnt = this.BASE.Variables.Where(r => r.Key.Contains("V_W_SUBEQP_ID")).Count();
                    if (subUnitCnt == 1)
                    {
                        string subEqpID = this.BASE.Variables["EQP_INFO:V_W_SUBEQP_ID"].AsString;
                        Array.Resize(ref this._arrTactEQPID, this.TactEQPIDs.Length + 1);
                        this.TactEQPIDs.SetValue(subEqpID, 1);
                    }
                    else if (subUnitCnt > 1)
                    {
                        for (int i = 1; i <= subUnitCnt; i++)
                        {
                            if (this.BASE.Variables.ContainsKey($"EQP_INFO:V_W_SUBEQP_ID_{i:D2}"))
                            {
                                Array.Resize(ref this._arrTactEQPID, this.TactEQPIDs.Length + 1);
                                this.TactEQPIDs.SetValue(this.BASE.Variables[$"EQP_INFO:V_W_SUBEQP_ID_{i:D2}"].AsString, i);
                            }
                        }
                    }
                    #endregion
                }

                return _arrTactEQPID;
            }
        }

        private CCommon m_Common;

        //$ 중요 : Modeler에 등록 된 Control Server의 이름으로 Key를 써야 하는데.. 귀찮으니 첫 번째 값을 쓰도록 하자. Base는 HPCD, Imp는 HPCDImp 이므로 무조건 First가 Base
        protected override CElement Owner => CExecutor.ElementsByElementPath.First().Value;
        private CSolaceEIFServerBizRule _EIFServer = null;

        private Dictionary<string, bool> NakPassList = null;
        private Dictionary<string, bool> TimeOutPassList = null;
        private Dictionary<string, string> PropertyDesc = null;
        private Dictionary<string, string> EventTxnID = null; //$ 2025.08.13 : TxnID를 저장할 Dictionary

        private object objBadcell = new object();

        private object _lockHostAlm_01 = new object();
        private object _lockHostAlm_02 = new object();
        private object _lockHostAlm_03 = new object();
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_SIXLOSSCODE_USE", "EQP_INFO", enumAccessType.Virtual, true, false, false, "", "True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용"); //$ 2023.07.26 : Loss Code 3자리 or 6자리 사용 여부

            //$ 2024.11.22 : Solace Request Queue 변수 추가
            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "BIZ_INFO", enumAccessType.Virtual, false, true, "", "", "EIF -> Biz Server Req Queue Name");
            __INTERNAL_VARIABLE_INTEGER("V_BIZCALL_TIMEOUT", "BIZ_INFO", enumAccessType.Virtual, 30000, 0, true, false, 0, string.Empty, "Biz Call TimeOut(mSec)");

            __INTERNAL_VARIABLE_SHORT("V_W_MAX_RETRY_CNT", "DEG_LD", enumAccessType.Virtual, 10, 0, true, true, 3, "", "Retry Wait Loop to aviod Degas Loader Job End Biz");
            __INTERNAL_VARIABLE_SHORT("V_W_WAITTIME", "DEG_LD", enumAccessType.Virtual, 10000, 0, true, true, 500, "", "Wait time(ms) for Avoiding TX Error");

            #region Factova Monitoring용 가상 변수
            __INTERNAL_VARIABLE_STRING("V_MONITOR_FACTORY", "MONITOR", enumAccessType.Virtual, false, true, "ESHG_EIF", "", "설비 공장명");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_CATEGORY", "MONITOR", enumAccessType.Virtual, false, true, "Form", "", "설비 카테고리");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQUIPMENT_ID", "MONITOR", enumAccessType.Virtual, false, true, "", "", "설비 ID");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQP_NICNAME", "MONITOR", enumAccessType.Virtual, false, true, "", "", "장비 NIC Name");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_DEVICETYPE", "MONITOR", enumAccessType.Virtual, false, true, "EIF", "", "설비 장비타입");

            __INTERNAL_VARIABLE_STRING("V_MONITOR_HOST_COMMUNICATIONSTATE", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Host와의 통신 상태 Online,Offline");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_PLC_COMMNUICATION", "MONITOR", enumAccessType.Virtual, false, false, "", "", "PLC와의 통신 상태");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_CIMSTATUS", "MONITOR", enumAccessType.Virtual, false, true, "", "", "CIM Online Status 상태 Auto,Pausing,Paused..Reconcileing");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_BIZ_VERSION", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Biz Version");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_SCAN_INTERVAL", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Scan Interval");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_BASE_HOSTNAME", "MONITOR", enumAccessType.Virtual, false, true, "", "", "MCCS 운영 기준 ENG01 또는 ENG02 의 Base HostName. 어떤 Node에서 운영중인지 확인 용도");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_NOTIFICATION", "MONITOR", enumAccessType.Virtual, false, true, "", "", "통합관리로 Risk 정보를 Notification");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_LOCAL_HOST_IP", "MONITOR", enumAccessType.Virtual, false, true, "", "", "MCS HSMS driver의 Local Host IP (virtual IP)");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_SOLACE", "MONITOR", enumAccessType.Virtual, false, false, "", "", "Solace 접속 상태");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_FACTOVA_VER", "MONITOR", enumAccessType.Virtual, false, true, "", "", "Factova Version");
            __INTERNAL_VARIABLE_STRING("V_MONITOR_EQPSTATUS", "MONITOR", enumAccessType.Virtual, false, false, "", "", "EQP Status Run, Wait, Trouble, User Stop");
            #endregion
        }

        protected override void OnInitializeCompleted()
        {
            base.OnInitializeCompleted();

            _EIFServer = (CSolaceEIFServerBizRule)Owner;
            _EIFServer.HandleEmptyStringByNull = true;

            //$ 2023.01.19 : Host Bit 초기화 시점을 Onstarted에서 OnInitializeCompleted로 변경
            _EIFServer.SetStatusLog("Factova Initialize Completed");
            _EIFServer.SetSolaceInfo(this.ReqQueue, this.RepQueue, this.BizTimeout);

            this.MONITOR__V_MONITOR_EQUIPMENT_ID = this.EQPID;
            this.MONITOR__V_MONITOR_EQP_NICNAME = _EIFServer.Description;  //$ 2025.10.31 : NICKNAEM 항목에 ControlServer Desctripion을 보여 주기로 함
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.ONLINE.ToString();
            this.MONITOR__V_MONITOR_BIZ_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.MONITOR__V_MONITOR_BASE_HOSTNAME = Environment.MachineName;
            this.MONITOR__V_MONITOR_LOCAL_HOST_IP = _EIFServer.GetHostIP(_EIFServer.Variables["SOLACE:CONNECTION_INFO"].ToString());
            this.MONITOR__V_MONITOR_FACTOVA_VER = Assembly.GetEntryAssembly().GetName().Version.ToString();

            if (_EIFServer.Drivers.Count < 1) return; //$ 2023.02.09 : Driver Setting이 안되어 있을 땐 초기화 Logic 진행 필요 없음

            this.MONITOR__V_MONITOR_SCAN_INTERVAL = string.Join(",", _EIFServer.Drivers[1].ScanInterval);

            _EIFServer.Drivers[1].ConnectionStateChanged += (driver, state) =>
            {
                try
                {
                    if (state == enumConnectionState.Connected) //driver 접속시 out bit reset 
                    {
                        var _var = this.BASE.Variables.Values.Where(r => r.AccessType == enumAccessType.Out && r.DataType == enumDataType.Boolean && r.Category.Name != "COMM" && r.Category.Name != "SYSTEM").ToList();

                        foreach (var item in _var)
                        {
                            item.AsBoolean = false;
                            _EIFServer.SetVarStatusLog(this.Name, item, $"{item.Name} : OFF (PLC Reconnected)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _EIFServer.SetErrorLog(this.Name, new CVariable(), ex);
                }
            };
        }

        protected override void OnStarted()
        {
            base.OnStarted();
            _EIFServer.SetStatusLog("EIF FactoryLync(L2) Started");
            m_Common = new CCommon(_EIFServer, this, SIMULATION_MODE);
            this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !this.BASE.HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF;

            _EIFServer.SetLanguageID(this.EQPID, this.BASE.EQP_OP_MODE_CHG_RPT_01__I_W_HMI_LANG_TYPE);  //$ 2023.12.14 : 프로그램 시작 시 PLC 사용 언어 설정

            EIFMonitoringData();

            //TacttimeReport(); //$ 2025.09.22 : Scheduler Interval 이후 호출 되는 것이 문제가 된다면 OnStarted에서 명시적으로 호출 후 이후 Interval대로 반복 호출 함
        }

        protected override void DefineHandlers()
        {
            base.DefineHandlers();

            #region EQP Area
            #region Common
            __EVENT_BOOLEANCHANGED(this.BASE.__HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF, __HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF_OnBooleanChanged);
            __EVENT_ON(this.BASE.__COMM_STAT_CHG_RPT__I_B_COMM_ON, __COMM_STAT_CHG_RPT__I_B_COMM_ON_OnVariableOn);
            __EVENT_ON(this.BASE.__COMM_STAT_CHG_RPT__I_B_COMM_OFF, __COMM_STAT_CHG_RPT__I_B_COMM_OFF_OnVariableOn);

            __EVENT_ON(this.BASE.__DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ, __DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ_OnVariableOn);

            __EVENT_BOOLEANCHANGED(this.BASE.__SMOKE_RPT__I_B_SMOKE_DETECT_REQ, __SMOKE_RPT__I_B_SMOKE_DETECT_REQ_OnBooleanChanged);

            //__EVENT_STRINGCHANGED(this.BASE.__G1_1_MTRL_MONITER_DATA__I_W_STAT_CHG_EVENT_CODE, __G1_1_MTRL_MONITER_DATA__I_W_STAT_CHG_EVENT_CODE_OnStringChanged);
            #endregion

            #region Unloader
            __EVENT_BOOLEANCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE, __EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_01__I_B_ALARM_SET_REQ, __ALARM_RPT_01__I_B_ALARM_SET_REQ_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_01__I_B_ALARM_RESET_REQ, __ALARM_RPT_01__I_B_ALARM_RESET_REQ_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G2_2_CARR_IN_RPT__I_B_TRAY_EXIST, __G2_2_CARR_IN_RPT__I_B_TRAY_EXIST_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT, __G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT, __G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT, __G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_01__I_W_EQP_STAT, __EQP_STAT_CHG_RPT_01__I_W_EQP_STAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT, __EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_01__I_W_HMI_LANG_TYPE, __EQP_OP_MODE_CHG_RPT_01__I_W_HMI_LANG_TYPE_OnShortChanged, true);
            #endregion

            #region Sealer
            __EVENT_BOOLEANCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE, __EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_02__I_B_ALARM_SET_REQ, __ALARM_RPT_02__I_B_ALARM_SET_REQ_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_02__I_B_ALARM_RESET_REQ, __ALARM_RPT_02__I_B_ALARM_RESET_REQ_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_01__I_B_CELL_EXIST_01, __G3_5_APD_RPT_01__I_B_CELL_EXIST_01_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_01__I_B_CELL_EXIST_02, __G3_5_APD_RPT_01__I_B_CELL_EXIST_02_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_01__I_B_TRIGGER_REPORT, __G3_5_APD_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G4_3_CELL_OUT_RPT_01__I_B_TRIGGER_REPORT, __G4_3_CELL_OUT_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_02__I_W_EQP_STAT, __EQP_STAT_CHG_RPT_02__I_W_EQP_STAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT, __EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_02__I_W_HMI_LANG_TYPE, __EQP_OP_MODE_CHG_RPT_02__I_W_HMI_LANG_TYPE_OnShortChanged, true);
            #endregion            

            #region HotPress   
            __EVENT_BOOLEANCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE, __EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_03__I_B_ALARM_SET_REQ, __ALARM_RPT_03__I_B_ALARM_SET_REQ_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_03__I_B_ALARM_RESET_REQ, __ALARM_RPT_03__I_B_ALARM_RESET_REQ_OnBooleanChanged);

            #region IV 관련  
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_02__I_B_CELL_EXIST_01, __G3_5_APD_RPT_02__I_B_CELL_EXIST_01_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_02__I_B_CELL_EXIST_02, __G3_5_APD_RPT_02__I_B_CELL_EXIST_02_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_02__I_B_TRIGGER_REPORT, __G3_5_APD_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G4_3_CELL_OUT_RPT_02__I_B_TRIGGER_REPORT, __G4_3_CELL_OUT_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged);
            #endregion

            #region HotPress 관련
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_03__I_B_CELL_EXIST_01, __G3_5_APD_RPT_03__I_B_CELL_EXIST_01_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_03__I_B_CELL_EXIST_02, __G3_5_APD_RPT_03__I_B_CELL_EXIST_02_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_03__I_B_TRIGGER_REPORT, __G3_5_APD_RPT_03__I_B_TRIGGER_REPORT_OnBooleanChanged);
            #endregion         

            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_03__I_W_EQP_STAT, __EQP_STAT_CHG_RPT_03__I_W_EQP_STAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT, __EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT_03__I_W_HMI_LANG_TYPE, __EQP_OP_MODE_CHG_RPT_03__I_W_HMI_LANG_TYPE_OnShortChanged, true);
            #endregion
            #endregion

            #region HOST Area
            #region Common
            __EVENT_BOOLEANCHANGED(this.BASE.__SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion           

            #region Unloader
            __EVENT_BOOLEANCHANGED(this.BASE.__HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_01__O_B_ALARM_SET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_01__O_B_ALARM_RESET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion

            #region Sealer
            __EVENT_BOOLEANCHANGED(this.BASE.__HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_02__O_B_ALARM_SET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_02__O_B_ALARM_RESET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion

            #region HotPress
            __EVENT_BOOLEANCHANGED(this.BASE.__HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_03__O_B_ALARM_SET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT_03__O_B_ALARM_RESET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);

            #region IV 관련
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion

            #region Hotpress 관련
            __EVENT_BOOLEANCHANGED(this.BASE.__G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion
            #endregion
            #endregion

            #region Remote Command
            #region Unloader
            __EVENT_SHORTCHANGED(this.BASE.__REMOTE_COMM_SND_01__V_REMOTE_CMD, __REMOTE_COMM_SND_01__V_REMOTE_CMD_OnShortChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_01__O_B_REMOTE_COMMAND_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_01__I_B_REMOTE_COMMAND_CONF, __REMOTE_COMM_SND_01__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged);
            #endregion

            #region Sealer
            __EVENT_SHORTCHANGED(this.BASE.__REMOTE_COMM_SND_02__V_REMOTE_CMD, __REMOTE_COMM_SND_02__V_REMOTE_CMD_OnShortChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_02__O_B_REMOTE_COMMAND_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_02__I_B_REMOTE_COMMAND_CONF, __REMOTE_COMM_SND_02__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged);
            #endregion

            #region HotPress
            __EVENT_SHORTCHANGED(this.BASE.__REMOTE_COMM_SND_03__V_REMOTE_CMD, __REMOTE_COMM_SND_03__V_REMOTE_CMD_OnShortChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_03__O_B_REMOTE_COMMAND_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND_03__I_B_REMOTE_COMMAND_CONF, __REMOTE_COMM_SND_03__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged);
            #endregion
            #endregion

            #region ETC
            __EVENT_BOOLEANCHANGED(this.BASE.__TESTMODE__V_IS_NAK_TEST, __TESTMODE_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__TESTMODE__V_IS_TIMEOUT_TEST, __TESTMODE_BIT_OnBooleanChanged);
            #endregion

            //$ 2025.09.22 : 기존 100ms 후 빠르게 Scheduler 함수 호출하고 Wait로 Interval 조정하던 것을 정상적인 Process로 처리(프로그램 시작하자마 호출 필요 시 따로 Scheduler 함수 호출)
            __SCHEDULER(TacttimeReport, this.BASE.EQP_INFO__V_TACTTIME_INTERVAL * 1000, true); //$ 2025.07.26 : 초기 Scheduler 시작 시간을 짧게 설정하고 해당 Scheduler 함수 안에서 Wait로 시간 조정
            //SchedulerHandlers.First().Value.Run(); //RunAtStartUp = falas일 경우 Thread Method 구동하는 방법
        }

        protected override void OnInstancingCompleted()
        {
            base.OnInstancingCompleted();

            this.EventTxnID = new Dictionary<string, string>(); //$ 2025.08.13 : TxnID를 저장할 Dictionary
            this.PropertyDesc = new Dictionary<string, string>(); //Desc에서 값을 한번이라도 읽어오면, 내부 Dictionary에서 찾게 하자..

            __MONITOR__V_MONITOR_FACTORY.SystemMonitoringInfo = new CVariableSystemMonitoringInfo() { CategoryLevel = 0, IsCategoryItem = true };
            __MONITOR__V_MONITOR_CATEGORY.SystemMonitoringInfo = new CVariableSystemMonitoringInfo() { CategoryLevel = 1, IsCategoryItem = true };

            __MONITOR__V_MONITOR_CATEGORY.SystemMonitoring = true;      //Modeler에서 입력
            __MONITOR__V_MONITOR_FACTORY.SystemMonitoring = true;       //Modeler에서 입력

            __MONITOR__V_MONITOR_EQUIPMENT_ID.SystemMonitoring = true;  // CImp에서
            __MONITOR__V_MONITOR_EQP_NICNAME.SystemMonitoring = true;   // ??
            __MONITOR__V_MONITOR_DEVICETYPE.SystemMonitoring = true;    //Modeler에서 입력

            __MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE.SystemMonitoring = true; // CImp에서 EIF가 켜지면 1, 꺼지면 0
            __MONITOR__V_MONITOR_PLC_COMMNUICATION.SystemMonitoring = true;       // Common에서 PLC 연결 상태 보고 1, 0
            __MONITOR__V_MONITOR_CIMSTATUS.SystemMonitoring = false;               // ??CIM이 없는데.

            __MONITOR__V_MONITOR_BIZ_VERSION.SystemMonitoring = true;              //CImp에서
            __MONITOR__V_MONITOR_SCAN_INTERVAL.SystemMonitoring = true;            //CImp에서

            __MONITOR__V_MONITOR_BASE_HOSTNAME.SystemMonitoring = true;             //CImp에서
            __MONITOR__V_MONITOR_NOTIFICATION.SystemMonitoring = true;              //??

            __MONITOR__V_MONITOR_LOCAL_HOST_IP.SystemMonitoring = true;
            __MONITOR__V_MONITOR_SOLACE.SystemMonitoring = true;
            __MONITOR__V_MONITOR_FACTOVA_VER.SystemMonitoring = true;
            __MONITOR__V_MONITOR_EQPSTATUS.SystemMonitoring = true;
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            this.MONITOR__V_MONITOR_SOLACE = enumConnectionState.Disconnected.ToString();
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.OFFLINE.ToString();
        }
        #endregion

        #region Event Method
        #region Virtual Event Method
        //$ 2023.03.24 : Nak Test와 TimeoutTest가 On된 적이 있다가 모두 Off될 경우 List를 Clear
        protected virtual void __TESTMODE_BIT_OnBooleanChanged(CVariable sender, bool value)
        {
            if (!this.BASE.TESTMODE__V_IS_NAK_TEST && !this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
            {
                if (this.NakPassList != null) this.NakPassList.Clear();
                if (this.TimeOutPassList != null) this.TimeOutPassList.Clear();
            }
        }
        #endregion

        #region Solace Event Method   
        public void OnMessageReceived(IMessage request, string topic, string message)
        {
            try
            {
                _EIFServer.SetLog($"[RCVDMSG] [Host Alarm Message Received]  Message: {message}", EifFileName, this.EQPID);

                HOSTMSG_SEND msg = JsonConvert.DeserializeObject<HOSTMSG_SEND>(message);

                // check EQP ID
                if (this.EQPID != msg.refDS.IN_DATA[0].EQPTID && this.SEALEREQPID != msg.refDS.IN_DATA[0].EQPTID && this.HOTPRESSEQPID != msg.refDS.IN_DATA[0].EQPTID)
                {
                    _EIFServer.SetWarnLog($"[RCVDMSG] [Received EQPID is not valid.]  Message: {message}", EifFileName, this.EQPID);
                    return;
                }

                // HMI Language Type 에 따른 Messge
                string rcvdAlarmMsg = string.Empty;
                switch (this.BASE.EQP_OP_MODE_CHG_RPT_01__I_W_HMI_LANG_TYPE)
                {
                    case GLOBAL_LANGUAGE.KOREA:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_KOR_1;
                        break;
                    case GLOBAL_LANGUAGE.ENGLISH:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_ENG_1;
                        break;
                    case GLOBAL_LANGUAGE.CHINA:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_CHN_1;
                        break;
                    case GLOBAL_LANGUAGE.POLAND:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_POL_1;
                        break;
                    case GLOBAL_LANGUAGE.UKRAINE:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_UKR_1;
                        break;
                    case GLOBAL_LANGUAGE.RUSSIA:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_RUS_1;
                        break;
                    case GLOBAL_LANGUAGE.INDONESIA:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_IDN_1;
                        break;
                    default:
                        rcvdAlarmMsg = msg.refDS.IN_DATA[0].MSGNAME_ENG_1;
                        break;
                }

                // Send System
                ushort sendSystem = 0;
                switch (msg.refDS.IN_DATA[0].SYS_NAME)
                {
                    case "APC":
                        sendSystem = 1;
                        break;
                    case "FDC":
                        sendSystem = 2;
                        break;
                    case "SPC+":
                        sendSystem = 3;
                        break;
                    default:
                        sendSystem = 0;     // MES
                        break;
                }

                // Host Alarm Action (stop type)
                ushort stop = Convert.ToUInt16(msg.refDS.IN_DATA[0].STOP_TYPE);

                // Unit 별 Host Alarm 처리
                if (msg.refDS.IN_DATA[0].EQPTID == this.EQPID)
                {
                    HostAlarm(EIFALMCD.DEFAULT, rcvdAlarmMsg, 1, true, sendSystem, stop);
                }
                else if (msg.refDS.IN_DATA[0].EQPTID == this.SEALEREQPID)
                {
                    SealerHostAlarm(EIFALMCD.DEFAULT, rcvdAlarmMsg, 1, true, sendSystem, stop);
                }
                else if (msg.refDS.IN_DATA[0].EQPTID == this.HOTPRESSEQPID)
                {
                    HotpressHostAlarm(EIFALMCD.DEFAULT, rcvdAlarmMsg, 1, true, sendSystem, stop);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.EQPID, this.EQPID);
            }
        }
        #endregion

        #region Bit Event Method
        #region Common
        private void __HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !value;
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __COMM_STAT_CHG_RPT__I_B_COMM_ON_OnVariableOn(CVariable sender)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (EQP Comm State : On(Normal State)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __COMM_STAT_CHG_RPT__I_B_COMM_OFF_OnVariableOn(CVariable sender)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (EQP Comm State : Off(AbNormal State)");
                _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (EQP Comm Check Retry)");
                this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !this.BASE.HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF;
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        //$ 2020.11.10 : 시간 동기화 요청 후 3초 후 Off
        private void __DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ_OnVariableOn(CVariable sender)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (Host - EQP System Time Sync Bit On)");

                Wait(3000);

                this.BASE.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ = false;

                _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (Host System Time Sync Bit Off)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        protected virtual void __SMOKE_RPT__I_B_SMOKE_DETECT_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.SMOKE_RPT__I_B_SMOKE_DETECT_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = value;

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF)}] : {this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_SET_FORM_FIRE_OCCUR_NEW";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_SET_FORM_FIRE_OCCUR_NEW_IN inData = CBR_SET_FORM_FIRE_OCCUR_NEW_IN.GetNew(this);
                CBR_SET_FORM_FIRE_OCCUR_NEW_OUT outData = CBR_SET_FORM_FIRE_OCCUR_NEW_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[0].IFMODE = IFMODE.ONLINE;
                inData.INDATA[0].USERID = USERID.EIF;

                inData.INDATA[0].EQPTID = this.EQPID;
                inData.INDATA[0].SMOKE_DETECT = this.BASE.SMOKE_RPT__I_W_EQP_SMOKE_STATUS.ToString();
                inData.INDATA[0].TRAY_EXIST = this.BASE.G2_2_CARR_IN_RPT__I_B_TRAY_EXIST ? "1" : "0";

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Smoke Detect Req No Input Data!!");

                    this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);

                    this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF)}] : Timeout Test");
                }
                else
                {
                    this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = true;
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.SMOKE_RPT__I_W_EQP_SMOKE_STATUS)}] : {this.BASE.SMOKE_RPT__I_W_EQP_SMOKE_STATUS}");
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF)}] : {this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion

        #region Unloader
        private void __EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE)}] {(value ? "Control" : "Maintenance")} Mode");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_01__I_B_ALARM_SET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__I_B_ALARM_SET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF)}] : {this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.EQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_01__I_W_ALARM_SET_ID.ToString("D6");
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.SET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Set Req No Input Data!!");

                    this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);

                    this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF)}] : true - {this.BASE.ALARM_RPT_01__I_W_ALARM_SET_ID}");
                    this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF = true;
                }

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_01__I_B_ALARM_RESET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__I_B_ALARM_RESET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF)}] : {this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.EQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_01__I_W_ALARM_RESET_ID.ToString("D6");

                // RESET시 ALARMID가 0인 경우 EQPT_ALARM_EVENT_TYPE은 값을 Mapping하지 않게 하여 NULL로 인식할 수 있게 하자.
                if (this.BASE.ALARM_RPT_01__I_W_ALARM_RESET_ID != 0)
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.RESET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Reset Req No Input Data!!");

                    this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);

                    this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF)}] : true - {this.BASE.ALARM_RPT_01__I_W_ALARM_RESET_ID}");
                    this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF = true;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G2_2_CARR_IN_RPT__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"{"EQP"} {nameof(this.BASE.G2_2_CARR_IN_RPT__I_B_TRAY_EXIST)} : {value}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_2_CARR_IN_RPT__O_W_TRIGGER_REPORT_ACK);

                string trayID = IsHR ? this.BASE.G2_2_CARR_IN_RPT__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_2_CARR_IN_RPT__I_W_TRAY_ID);
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value} | {nameof(this.BASE.G2_2_CARR_IN_RPT__I_W_TRAY_ID)} : {trayID}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (this.BASE.G2_2_CARR_IN_RPT__I_B_TRAY_EXIST == false)
                {
                    _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"Tray Exist : {(this.BASE.G2_2_CARR_IN_RPT__I_B_TRAY_EXIST ? "EXISTS" : "EMPTY")}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //2025-05-23 하유승
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_2_CARR_IN_RPT__I_W_TRAY_ID))} : {trayID}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_ULD_TRAY_ARRIVED";
                Exception bizEx = null;

                CBR_SET_DEGAS_ULD_TRAY_ARRIVED_IN inData = CBR_SET_DEGAS_ULD_TRAY_ARRIVED_IN.GetNew(this);
                CBR_SET_DEGAS_ULD_TRAY_ARRIVED_OUT outData = CBR_SET_DEGAS_ULD_TRAY_ARRIVED_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].ULD_CSTID = trayID;

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"ULD Tray BCR Read Req1 No Input Data!! {this.EQPID} {trayID}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call -> Degas UnLoader Tray Arrived
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    if (!this.BASE.G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT)
                    {
                        _EIFServer.SetVarStatusLog(this.Name, sender, $"DEGAS ULD Tray ID Report check Confirm,But {I_B_REQ} {this.BASE.G2_2_CARR_IN_RPT__I_B_TRIGGER_REPORT}");

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                        return;
                    }

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    string sLog = $"DEGAS ULD Tray ID Report Confirm ACK: [Tray ID] {trayID} {O_B_REP} {this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    string sLog = $"DEGAS ULD Tray ID Report Confirm NAK : [Tray ID] {trayID} {O_B_REP} {this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }

                //$ 2022.12.21 : 설비로 투입되는 Tray에 대해 TrayID를 MHS로 보고하여 반송 종료 처리 함
                _EIFServer.MhsReport_LoadedCarrier(SIMULATION_MODE, this.EQPID, trayID);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_2_CARR_IN_RPT__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        private void __G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_3_CARR_OUT_RPT__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (this.BASE.G2_3_CARR_OUT_RPT__I_B_TRAY_EXIST == false)
                {
                    _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"Tray Exist : {(this.BASE.G2_3_CARR_OUT_RPT__I_B_TRAY_EXIST ? "EXISTS" : "EMPTY")}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    this.BASE.G2_3_CARR_OUT_RPT__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                    this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF = true;

                    return;
                }

                string trayID = IsHR ? this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_ID);
                //2025-05-23 하유승
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_ID))} : {trayID}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_ULD_TRAY_JOB_END";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_SET_DEGAS_ULD_TRAY_JOB_END_IN inData = CBR_SET_DEGAS_ULD_TRAY_JOB_END_IN.GetNew(this);
                CBR_SET_DEGAS_ULD_TRAY_JOB_END_OUT outData = CBR_SET_DEGAS_ULD_TRAY_JOB_END_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                inData.SUBLOTDATA_LENGTH = 0;

                // int iCellCnt = this.Get_Tray_Type_Cnt1(sender, this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_ID);

                int iCellCnt = this.BASE.EQP_INFO__V_TRAYINCELLCNT;

                List<string> arrCellIDList = this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_IN_CELLID_LIST;  //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함

                for (int i = 0; i < iCellCnt; i++)
                {
                    inData.SUBLOTDATA_LENGTH++;
                    inData.SUBLOTDATA[inData.SUBLOTDATA_LENGTH - 1].CSTSLOT = (i + 1).ToString();
                    inData.SUBLOTDATA[inData.SUBLOTDATA_LENGTH - 1].SUBLOTID = IsHR ? (arrCellIDList[i] == string.Empty ? "0000000000" : arrCellIDList[i].Trim()) : (_EIFServer.GetSimArrValue(() => this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_IN_CELLID_LIST, i));
                    if (inData.SUBLOTDATA[inData.SUBLOTDATA_LENGTH - 1].SUBLOTID != "0000000000")
                    {
                        inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOT_COUNT += 1;
                    }
                }

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0 || inData.SUBLOTDATA.Count == 0)
                {
                    _EIFServer.SetStatusLog($"Tray Job Complete1 No Input Data!! {this.EQPID} {trayID}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call -> JobComplete
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    //작업종료 성공
                    if (!this.BASE.G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT)
                    {
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                        sLog = $"HOST Job Complete confirmed, But {eventName} [{I_B_REQ}] : {this.BASE.G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT}";
                        _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                        return;
                    }

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF} - [Tray ID] {trayID}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    sLog = $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF} - [Tray ID] {trayID}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_3_CARR_OUT_RPT__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        private void __G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK);

                if (!value)
                {
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName}  [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                //2025-05-23 하유승
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_01__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                List<string> arrCellIDList = this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST; //$ 2024.03.07 : 배열에 대한 값을 Read/Write할때 따로 변수 처리를 해야 부하나 속도 이슈가 없다고 함, Log 찍는 위치 변경
                _EIFServer.SetStatusLog($"EQP {eventName} -> [{I_B_REQ}] : {value} {nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST)} : {arrCellIDList.Aggregate((cellid, next) => cellid + ", " + next)}");

                //2025-05-23 하유승
                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST))} : {arrCellIDList.Aggregate((cellID, next) => cellID + ", " + next)}");

                int iRst = 0;
                string strBizName = "BR_GET_DEGAS_ULD_CELL_CHECK";
                Exception bizEx = null;

                CBR_GET_DEGAS_ULD_CELL_CHECK_IN inData = CBR_GET_DEGAS_ULD_CELL_CHECK_IN.GetNew(this);
                CBR_GET_DEGAS_ULD_CELL_CHECK_OUT outData = CBR_GET_DEGAS_ULD_CELL_CHECK_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].FIRST_SUBLOTID = IsHR ? this.BASE.G4_6_CELL_INFO_REQ__I_W_FIRST_CELL_ID : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_FIRST_CELL_ID);
                inData.CELLDATA_LENGTH = 0;
                for (int i = 0; i < arrCellIDList.Count; i++)
                {
                    if (!string.IsNullOrEmpty(arrCellIDList[i]))
                    {
                        inData.CELLDATA_LENGTH++;
                        inData.CELLDATA[inData.CELLDATA_LENGTH - 1].SUBLOTID = IsHR ? arrCellIDList[i] : _EIFServer.GetSimArrValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST, i); //$ 2024.03.07 : 기존 Factova 변수에 배열값을 직접 Read하던 부분을 지역변수로 변경했음, SimulData는 PLC와 무관하므로 기존 방식 유지
                    }
                }

                #region $ 2024.08.13 : Indata CellList 보고 추가
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // $ 2024.07.11 : Biz 실행 결과 Mix된 상태의 경우 Pass를 내려 Tray를 배출하게 함 => 해당 강제 배출을 사용하기 위해 Indata로 Tray의 Cell정보 Update가 선행 되어야 함
                // UC2의 경우 새로운 CellList[72]를 추가했는데.. YTS의 TrayCellList Update시점에 문제가 있어서 추가된 사양이라고 하며 불필요한 Address 낭비를 막기 위해 
                // 기존 TrayCellList Update 시점을 ESST 사전 검수 시 YTS 담당자와 확인하였으며, 기존 TrayCellList로 일원화 하기로 최종 확정 했음
                // 추후 YTS 설비인 경우 ESST PLC 프로그램인지 확인 필요, YTS업체가 아닌 경우 CELL_INFO__I_B_CELL_BCR_READ_REQ 이후 TrayCellList가 Update되는지 확인 필요
                // YTS의 경우 CELL_INFO__I_B_CELL_BCR_READ_REQ를 하기 전에 이미 TrayCellList가 Update되어 실물이 이동하기 전인데 정보상으로 TrayCellList에 Cell이 있어 강제 배출이
                // 불필요하다고 GMES에서 판단하게 되었으며, 이로 인해 Cell이 혼입 되었음. 결과적으로 L등급 Cell 혼입을 막기 위한 Logic이 무의미해졌을 것으로 예상, 이를 피하고자 신규 CellList[72]가 추가됨
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                int iCellCnt = this.BASE.EQP_INFO__V_TRAYINCELLCNT;
                List<string> arrTrayCellIDList = this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_IN_CELLID_LIST;

                for (int i = 0; i < iCellCnt; i++)
                {
                    inData.CELLLISTDATA_LENGTH++;
                    inData.CELLLISTDATA[inData.CELLLISTDATA_LENGTH - 1].SUBLOTID = IsHR ? arrTrayCellIDList[i].Trim() : _EIFServer.GetSimArrValue(() => this.BASE.G2_3_CARR_OUT_RPT__I_W_TRAY_IN_CELLID_LIST, i);
                    if (inData.CELLLISTDATA[inData.CELLLISTDATA_LENGTH - 1].SUBLOTID.Equals("0000000000"))
                    {
                        inData.CELLLISTDATA[inData.CELLLISTDATA_LENGTH - 1].SUBLOTID = string.Empty;
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"ULD Cell#1 BCR Read Req No Input Data!! {this.EQPID}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call -> Degas UnLoader Cell#1 Arrived
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 2);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    //$ 2024.07.11 : Biz 실행 결과 Mix된 상태의 경우 Pass를 내려 Tray를 배출하게 함
                    if (outData.OUTDATA[0].MIX_CODE == "1")
                    {
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.PASS, txnID);

                        _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST Pass {eventName}  [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF} |  {nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST)} : {arrCellIDList.Aggregate((cellID, next) => cellID + ", " + next)}");
                    }
                    else
                    {
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                        _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST ACK {eventName}  [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF} |  {nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST)} : {arrCellIDList.Aggregate((cellID, next) => cellID + ", " + next)}");
                    }
                }
                else
                {
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST NAK {eventName}  [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF} |  {nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELLID_LIST)} : {arrCellIDList.Aggregate((cellID, next) => cellID + ", " + next)}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion


        #region Sealer
        private void __EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE)}] {(value ? "Control" : "Maintenance")} Mode");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_02__I_B_ALARM_SET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__I_B_ALARM_SET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF)}] : {this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.SEALEREQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_02__I_W_ALARM_SET_ID.ToString("D6");
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.SET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Set Req No Input Data!!");

                    this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF = true;  //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);    //$ 2024.08.12 : 세부 Unit으로 Host Alarm 발생 변경

                    this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF = true;  //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF)}] : true - {this.BASE.ALARM_RPT_02__I_W_ALARM_SET_ID}");
                    this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF = true;
                }

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_02__I_B_ALARM_RESET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__I_B_ALARM_RESET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF)}] : {this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.SEALEREQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_02__I_W_ALARM_RESET_ID.ToString("D6");

                // RESET시 ALARMID가 0인 경우 EQPT_ALARM_EVENT_TYPE은 값을 Mapping하지 않게 하여 NULL로 인식할 수 있게 하자.
                if (this.BASE.ALARM_RPT_02__I_W_ALARM_RESET_ID != 0)
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.RESET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Reset Req No Input Data!!");

                    this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF = true;  //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);    //$ 2024.08.12 : 세부 Unit으로 Host Alarm 발생 변경

                    this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF = true;  //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF)}] : true - {this.BASE.ALARM_RPT_02__I_W_ALARM_RESET_ID}");
                    this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF = true;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G3_5_APD_RPT_01__I_B_CELL_EXIST_01_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, "Cell Arrive 1 : ", value);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G3_5_APD_RPT_01__I_B_CELL_EXIST_02_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, "Cell Arrive 2 : ", value);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G3_5_APD_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G3_5_APD_RPT_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G3_5_APD_RPT_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.SEALEREQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.SEALEREQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.SEALEREQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST) // 2024.03.07 HDH : OPERMODE SEALER로 변경
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.SEALEREQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (this.BASE.G3_5_APD_RPT_01__I_B_CELL_EXIST_01 == false && this.BASE.G3_5_APD_RPT_01__I_B_CELL_EXIST_02 == false)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"[{this.SEALEREQPID}] Cell ID Report : Cell Not Exist");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string cellID1 = IsHR ? this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_01.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_01);
                string cellID2 = IsHR ? this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_02.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_02);
                SolaceLog(this.SEALEREQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_01))} : {cellID1}, {GetDesc(nameof(this.BASE.G3_5_APD_RPT_01__I_W_CELL_ID_02))} : {cellID2}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_MAINSEALING_CELL_DATA";
                Exception bizEx = null;
                string sLog = string.Empty;

                //SET_DEGAS_MAINSEALING_CELL_DATA
                CBR_SET_DEGAS_MAINSEALING_CELL_DATA_IN inData = CBR_SET_DEGAS_MAINSEALING_CELL_DATA_IN.GetNew(this);
                CBR_SET_DEGAS_MAINSEALING_CELL_DATA_OUT outData = CBR_SET_DEGAS_MAINSEALING_CELL_DATA_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                if (this.BASE.G3_5_APD_RPT_01__I_B_CELL_EXIST_01)
                {
                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.DEGASEQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CELL_POSITION = "1";
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = cellID1;

                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_LOCATION_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_NO1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_NO1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_POS1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_POS1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_NEST_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_NEST_NO1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_NEST_NO1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].DEGAS_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_VAL1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_VAL1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].DEGAS_PRESS_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_REACH_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_REACH_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_REACH_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_KEEP_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_KEEP_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_KEEP_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_REL_TIME = 0;
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_DGR_VALUE = 0;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_VACM_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_DEGREE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_DEGREE1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].APRS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_APRS_VALUE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_APRS_VALUE1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_VENT_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_VENT_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_CYCL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_CYCLE_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_CYCLE_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP1_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP1_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP2_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP2_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP3_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP3_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP1_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP1_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP2_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP2_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP3_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP3_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_PRESSURE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_PRESSURE1);

                    inData.INDATA[inData.INDATA_LENGTH - 1].MNUS_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_MINUS_VENT_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_MINUS_VENT_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_1ST_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_1ST_VENT_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_1ST_VENT_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_HOLD_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VENT_HOLD_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VENT_HOLD_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_2ND_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_2ND_VENT_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_2ND_VENT_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].ABS_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_ABS_PRESS_VALUE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_ABS_PRESS_VALUE1);

                    inData.INDATA[inData.INDATA_LENGTH - 1].RWK_FLAG = this.BASE.G3_5_APD_RPT_01__I_B_CELL_REWORK ? "Y" : "N";
                }

                if (this.BASE.G3_5_APD_RPT_01__I_B_CELL_EXIST_02)
                {
                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.DEGASEQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CELL_POSITION = "2";
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = cellID2;

                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_LOCATION_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_NO2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_NO2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_POS2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_POS2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_NEST_NO = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_NEST_NO2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_NEST_NO2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].DEGAS_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_VAL2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_VAL2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].DEGAS_PRESS_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_DEGAS_PRESS_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_REACH_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_REACH_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_REACH_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_KEEP_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_KEEP_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_KEEP_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_REL_TIME = 0;
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_DGR_VALUE = 0;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_VACM_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_DEGREE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_DEGREE2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].APRS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_APRS_VALUE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_APRS_VALUE2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_VENT_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VACUUM_VENT_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].CHAMBER_CYCL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_CYCLE_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_CHAMBER_CYCLE_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP1_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP1_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP2_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP2_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TOP_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP3_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_UPPER_TEMP3_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP1_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP1_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP2_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP2_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_BTM_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP3_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_LOWER_TEMP3_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].PRE_SEAL_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_PRESSURE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_PRE_SEALING_PRESSURE2);

                    inData.INDATA[inData.INDATA_LENGTH - 1].MNUS_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_MINUS_VENT_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_MINUS_VENT_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_1ST_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_1ST_VENT_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_1ST_VENT_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_HOLD_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_VENT_HOLD_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_VENT_HOLD_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].VACM_2ND_VENT_TIME = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_2ND_VENT_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_2ND_VENT_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].ABS_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_01_DATA__I_W_ABS_PRESS_VALUE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_01_DATA__I_W_ABS_PRESS_VALUE2);

                    inData.INDATA[inData.INDATA_LENGTH - 1].RWK_FLAG = this.BASE.G3_5_APD_RPT_01__I_B_CELL_REWORK ? "Y" : "N";
                }

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    sLog = $"Cell#1 BCR Read Req No Input Data!! {this.SEALEREQPID} CellID#1 : {cellID1}, CellID#2 : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //JH 2025.08.04 TK 방지를 위한 Delay 조건 추가 
                AvoidTXRevErr(sender);

                //BizRule Call -> Degas Cell#1 Arrived (Degas Chamber 작업 Cell Data 보고)
                iRst = BizCall(strBizName, this.SEALEREQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    this.SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF} - [CELL ID#1] : {cellID1}, [CELL ID#2] : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
                else
                {
                    ushort uTroublePos = Convert.ToUInt16(outData.OUTDATA[0].TROUBLE_POSITION);

                    //$ 2020.11.02 : Trouble Posion을 빼고 Alarm Type에 해당 내역을 넣는데.. 1,2,3으로 오는 지 확인 필요, 정상인데도 Trouble Position을 줄 수 있는지 확인 필요
                    this.SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, uTroublePos, true);

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    sLog = $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF} - [CELL ID#1] : {cellID1}, [CELL ID#2] : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G3_5_APD_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        private void __G4_3_CELL_OUT_RPT_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_3_CELL_OUT_RPT_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_3_CELL_OUT_RPT_01__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.SEALEREQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.SEALEREQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.SEALEREQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST) // 2024.03.07 HDH : OPERMODE SEALER로 변경
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.SEALEREQPID, O_B_REP, O_W_REP_ACK); // 2024.03.07 HDH : ACK 추가
                        if (bOccur) return;
                    }
                }

                string cellID1 = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_01.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_01);
                string cellID2 = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_02.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_02);
                SolaceLog(this.SEALEREQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_01))} : {cellID1}, {GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_ID_02))} : {cellID2}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_CELL_OUTPUT";
                Exception bizEx = null;

                CBR_SET_DEGAS_CELL_OUTPUT_IN inData = CBR_SET_DEGAS_CELL_OUTPUT_IN.GetNew(this);
                CBR_SET_DEGAS_CELL_OUTPUT_OUT outData = CBR_SET_DEGAS_CELL_OUTPUT_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;
                inData.IN_SUBLOT_LENGTH = 0;

                for (ushort i = 0; i < 2; i++)
                {
                    string cellID = string.Empty;
                    string dfctcd = string.Empty;
                    string outinfo = string.Empty;

                    if (i == 0)
                    {
                        cellID = cellID1;
                        dfctcd = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_01.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_01).ToString();
                        outinfo = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_01.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_01).ToString();
                    }
                    else
                    {
                        cellID = cellID2;
                        dfctcd = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_02.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_02).ToString();
                        outinfo = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_02.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_02).ToString();
                    }

                    //CELLID가 NULL일 경우 INDATA에 넣지 않음.
                    if (string.IsNullOrWhiteSpace(cellID))
                    {
                        continue;
                    }

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.SEALEREQPID;

                    inData.IN_SUBLOT_LENGTH++;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].SUBLOTID = cellID;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].EQPT_DFCT_CODE = dfctcd;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].OUTPUT_RSLT_INFO = outinfo;
                }

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName}  No Input Data!! : BAD_CELL_ID1=[{cellID1}], BAD_CELL_ID2=[{cellID2}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //JH 2025.08.04 TK 방지를 위한 Delay 조건 추가 
                AvoidTXRevErr(sender);

                //BizRule Call -> DEGAS SUBLOT 배출로직
                lock (objBadcell)
                {
                    iRst = BizCall(strBizName, this.SEALEREQPID, inData, outData, out bizEx, txnID);
                }

                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    this.SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(this.SEALEREQPID, strBizName, inData.Variable, outData.Variable);

                HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                string sLog = $"EQPID ACK: {this.SEALEREQPID} , BAD Cell ID Report Confirm : Bad_Cell_ID1 : {cellID1}, Bad_Output_Line1 : {this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_01} , Bad_Cell_Judge1 : {this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_01} | Bad_Cell_ID2 : {cellID2}, Bad_Output_Line2 : {this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_RESULT_02} , Bad_Cell_Judge2 : {this.BASE.G4_3_CELL_OUT_RPT_01__I_W_CELL_OUT_DEFECTCD_02}";
                _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G4_3_CELL_OUT_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region HotPress
        #region IV 관련
        private void __G3_5_APD_RPT_02__I_B_CELL_EXIST_01_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{nameof(this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_01)} : {value}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __G3_5_APD_RPT_02__I_B_CELL_EXIST_02_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{nameof(this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_02)} : {value}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __G3_5_APD_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G3_5_APD_RPT_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G3_5_APD_RPT_02__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.SEALEREQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT1 = 0;
                    this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE1 = "";

                    this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT2 = 0;
                    this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE2 = "";

                    HostReply(this.SEALEREQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.SEALEREQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST) // 2024.03.07 HDH : OPERMODE 조건 SEALER로 변경
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.SEALEREQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_01 == false && this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_02 == false)
                {
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"[{this.SEALEREQPID}] IV Cell ID Report : Cell Not Exist");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string cellID1 = IsHR ? this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_01.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_01);
                string cellID2 = IsHR ? this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_02.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_02);
                SolaceLog(this.SEALEREQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_01))} : {cellID1}, {GetDesc(nameof(this.BASE.G3_5_APD_RPT_02__I_W_CELL_ID_02))} : {cellID2}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_MAINSEALING_CELL_DATA";
                Exception bizEx = null;
                string sLog = string.Empty;

                //SET_DEGAS_MAINSEALING_CELL_DATA
                CBR_SET_DEGAS_MAINSEALING_CELL_DATA_IN inData = CBR_SET_DEGAS_MAINSEALING_CELL_DATA_IN.GetNew(this);
                CBR_SET_DEGAS_MAINSEALING_CELL_DATA_OUT outData = CBR_SET_DEGAS_MAINSEALING_CELL_DATA_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_01)
                {
                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.SEALEREQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CELL_POSITION = "1";
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = cellID1;

                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_LOCATION_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_NO1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_NO1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP1_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP1_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP2_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP2_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP3_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP3_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP1_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP1_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP2_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP2_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP3_1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP3_1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_TIME1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_TIME1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_PRESSURE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_PRESSURE1);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOAD_CELL_PRESSURE1 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOAD_CELL_PRESSURE1);

                    inData.INDATA[inData.INDATA_LENGTH - 1].RWK_FLAG = this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK ? "Y" : "N";

                    inData.INDATA[inData.INDATA_LENGTH - 1].IV_MAINSEALING_REPORT_USE_FLAG = 1;
                }

                if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_02)
                {
                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.SEALEREQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CELL_POSITION = "2";
                    inData.INDATA[inData.INDATA_LENGTH - 1].SUBLOTID = cellID2;

                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_LOCATION_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_NO2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_NO2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP1_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP1_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP2_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP2_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TOP_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP3_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_UPPER_TEMP3_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR1_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP1_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP1_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR2_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP2_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP2_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_BTM_TMPR3_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP3_2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOWER_TEMP3_2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_TIME = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_TIME2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_TIME2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_PRESS_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_PRESSURE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_PRESSURE2);
                    inData.INDATA[inData.INDATA_LENGTH - 1].MAIN_SEAL_WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOAD_CELL_PRESSURE2 : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_MAIN_SEALING_LOAD_CELL_PRESSURE2);

                    inData.INDATA[inData.INDATA_LENGTH - 1].RWK_FLAG = this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK ? "Y" : "N";

                    inData.INDATA[inData.INDATA_LENGTH - 1].IV_MAINSEALING_REPORT_USE_FLAG = 1;
                }

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    sLog = $"Cell#1 BCR Read Req No Input Data!! {this.SEALEREQPID} CellID#1 : {cellID1}, CellID#2 : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //JH 2025.08.04 TK 방지를 위한 Delay 조건 추가 
                AvoidTXRevErr(sender);

                //BizRule Call -> Main Sealing 작업 Cell Data 보고
                iRst = BizCall(strBizName, this.SEALEREQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);   //$ 2024.08.12 : IV는 Sealer 제어 영역이므로 변경(Hotpress->Sealer)

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(this.SEALEREQPID, strBizName, inData.Variable, outData.Variable);

                int iRst2 = 0;
                string strBizName2 = "BR_SET_DEGAS_IV_CELL_DATA";
                Exception bizEx2 = null;
                string sLog2 = string.Empty;

                CBR_SET_DEGAS_IV_CELL_DATA_IN inData2 = CBR_SET_DEGAS_IV_CELL_DATA_IN.GetNew(this);
                CBR_SET_DEGAS_IV_CELL_DATA_OUT outData2 = CBR_SET_DEGAS_IV_CELL_DATA_OUT.GetNew(this);
                inData2.IN_EQP_LENGTH = 0;

                if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_01)
                {
                    inData2.IN_EQP_LENGTH++;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].EQPTID = this.SEALEREQPID;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SUBLOT_POSITION = "1";
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SUBLOTID = cellID1;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_DATA1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_DATA1).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].WEIGHT_MEASR_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_POS1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_POS1).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IVLTG_VALUE = "0";
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IVLTG_MEASR_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_POS1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_POS1).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].DEGAS_JUDG_RSLT_CODE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_JUDG1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_JUDG1).ToString();  // IV 절연전압 측정 결과 (1: 양품, 2: 절연전압불량, 3: 무게불량)

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].RWK_FLAG = IsHR ? (this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK ? "Y" : "N") : (_EIFServer.GetSimValue<bool>(() => this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK) ? "Y" : "N");

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IR_MEASR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_DATA1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_DATA1).ToString(); // ESHG IV값 사라지고 IV 어드레스에 IR 값으로 대체 됨.
                }

                if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_02)
                {
                    inData2.IN_EQP_LENGTH++;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].EQPTID = this.SEALEREQPID;
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SUBLOT_POSITION = "2";
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].SUBLOTID = cellID2;

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_DATA2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_DATA2).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].WEIGHT_MEASR_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_POS2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_WEIGHT_POS2).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IVLTG_VALUE = "0";
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IVLTG_MEASR_PSTN_NO = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_POS2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_POS2).ToString();
                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].DEGAS_JUDG_RSLT_CODE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_JUDG2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_JUDG2).ToString();

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].RWK_FLAG = IsHR ? (this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK ? "Y" : "N") : (_EIFServer.GetSimValue<bool>(() => this.BASE.G3_5_APD_RPT_02__I_B_CELL_REWORK) ? "Y" : "N");

                    inData2.IN_EQP[inData2.IN_EQP_LENGTH - 1].IR_MEASR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_DATA2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02_DATA__I_W_IV_DATA2).ToString(); //  ESHG IV값 사라지고 IV 어드레스에 IR 값으로 대체 됨.
                }

                // 입력 데이터가 없으면.
                if (inData2.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"IV Cell#1 BCR Read Req No Input Data!! {this.SEALEREQPID} CellID#1 : {cellID1}, CellID#2 : {cellID2}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call -> Degas Hotpress IV 작업 Cell Data 보고 
                iRst = BizCall(strBizName2, this.SEALEREQPID, inData2, outData2, out bizEx2, txnID);
                if (iRst2 != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName2, string.Empty, inData, bizEx2);

                    _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName2} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(strBizName2, this.SEALEREQPID, sender, bizEx2, 0, true);    //$ 2024.08.12 : IV는 Sealer 제어 영역이므로 변경(Hotpress->Sealer)

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(this.SEALEREQPID, strBizName2, inData2.Variable, outData2.Variable);

                if (outData2.OUTDATA[0].RETVAL == 0)
                {
                    // 등급 불량 배출 : 0 (Pass 양품), 1 (NG 불량배출)
                    List<ushort> lstGradePass = new List<ushort>();
                    List<string> lstGradeCD = new List<string>();
                    int nGradePassCnt = 0; // HDH 2023.08.25 : CELL 2번 단독으로 투입되는경우 판정 위치 오류 수정
                    for (int i = 0; i < outData2.CELLDATA.Count; i++)
                    {
                        lstGradePass.Add(Convert.ToUInt16(outData2.CELLDATA[i].GRADE_PASS));
                        lstGradeCD.Add(outData2.CELLDATA[i].GRADE_CD);

                        string sGradeCode = outData2.CELLDATA[i].GRADE_CD;
                        string sPass = outData2.CELLDATA[i].GRADE_PASS.ToString();
                        string sPassResult = int.Parse(sPass) == 0 ? "Pass" : "NG";

                        _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"Cell #{(i + 1).ToString()} : [Cell ID : {outData2.CELLDATA[i].SUBLOTID}], [Grade : {sGradeCode}], [Grade Exit : {sPassResult}]");
                    }

                    if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_01) // HDH 2023.08.25 : CELL 1번 존재하는 경우
                    {
                        this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT1 = IsRl ? lstGradePass[nGradePassCnt] : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT1);
                        this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE1 = IsRl ? lstGradeCD[nGradePassCnt] : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE1);
                        nGradePassCnt++; // HDH 2023.08.25 : CELL 1번,2번 둘다 존재하는 경우를 위해 nGradePassCnt 증가
                    }

                    if (this.BASE.G3_5_APD_RPT_02__I_B_CELL_EXIST_02) // HDH 2023.08.25 : CELL 2번 존재하는 경우
                    {
                        this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT2 = IsRl ? lstGradePass[nGradePassCnt] : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__O_W_CELL_JUDG_RESULT2);
                        this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE2 = IsRl ? lstGradeCD[nGradePassCnt] : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_02__O_W_CELL_GRADE2);
                    }

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF} - [CELL ID1] : {cellID1} - [CELL ID2] : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, sLog);
                }
                else
                {
                    ushort uTroublePos = Convert.ToUInt16(outData2.OUTDATA[0].TROUBLE_POSITION);
                    //$ 2020.11.02 : Trouble Posion을 빼고 Alarm Type에 해당 내역을 넣는데.. 1,2,3으로 오는 지 확인 필요, IV는 4,5,6써야 해서 기본 3에서 더함
                    SealerHostBizAlarm(strBizName2, this.SEALEREQPID, sender, bizEx2, (ushort)(3 + uTroublePos)); //$ 2024.08.12 : IV는 Sealer 제어 영역이므로 변경(Hotpress->Sealer)

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    sLog = $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF} - [CELL ID1] : {cellID1} - [CELL ID2] : {cellID2}";
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, sLog);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
                _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G3_5_APD_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        private void __G4_3_CELL_OUT_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_3_CELL_OUT_RPT_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_3_CELL_OUT_RPT_02__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.SEALEREQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.SEALEREQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.SEALEREQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST) // 2024.03.07 HDH : OPERMODE 조건 SEALER로 변경
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_02__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.SEALEREQPID, O_B_REP, O_W_REP_ACK); // 2024.03.07 HDH : ACK 값 추가
                        if (bOccur) return;
                    }
                }

                string cellID1 = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_01.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_01);
                string cellID2 = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_02.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_02);
                SolaceLog(this.SEALEREQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_01))} : {cellID1}, {GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_02))} : {cellID2}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_CELL_OUTPUT";
                Exception bizEx = null;

                CBR_SET_DEGAS_CELL_OUTPUT_IN inData = CBR_SET_DEGAS_CELL_OUTPUT_IN.GetNew(this);
                CBR_SET_DEGAS_CELL_OUTPUT_OUT outData = CBR_SET_DEGAS_CELL_OUTPUT_OUT.GetNew(this);

                inData.INDATA_LENGTH = 0;
                inData.IN_SUBLOT_LENGTH = 0;

                for (ushort i = 0; i < 2; i++)
                {
                    string cellID = string.Empty;
                    string dfctcd = string.Empty;
                    string outinfo = string.Empty;

                    if (i == 0)
                    {
                        cellID = cellID1;
                        dfctcd = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_01.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_01).ToString();
                        outinfo = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_01.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_01).ToString();
                    }
                    else
                    {
                        cellID = cellID2;
                        dfctcd = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_02.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_02).ToString();
                        outinfo = IsHR ? this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_02.ToString() : _EIFServer.GetSimValue(() => this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_02).ToString();
                    }

                    //CELLID가 NULL일 경우 INDATA에 넣지 않음.
                    if (string.IsNullOrWhiteSpace(cellID))
                    {
                        continue;
                    }

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.SEALEREQPID;

                    inData.IN_SUBLOT_LENGTH++;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].SUBLOTID = cellID;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].EQPT_DFCT_CODE = dfctcd;
                    inData.IN_SUBLOT[inData.INDATA_LENGTH - 1].OUTPUT_RSLT_INFO = outinfo;
                }

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(sender, $"{strBizName}  No Input Data!! : BAD_CELL_ID1=[{cellID1}], BAD_CELL_ID2=[{cellID2}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //JH 2025.08.04 TK 방지를 위한 Delay 조건 추가 
                AvoidTXRevErr(sender);

                //BizRule Call -> Degas IV 작업 Cell 불량 배출 보고
                lock (objBadcell)
                {
                    iRst = BizCall(strBizName, this.SEALEREQPID, inData, outData, out bizEx, txnID);
                }

                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(strBizName, this.SEALEREQPID, sender, bizEx, 0);   //$ 2024.08.12 : IV는 Sealer 제어 영역이므로 변경(Hotpress->Sealer)

                    HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(this.SEALEREQPID, strBizName, inData.Variable, outData.Variable);

                HostReply(this.SEALEREQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                string sLog = $"EQPID ACK: {this.SEALEREQPID} , BAD Cell ID Report Confirm : Bad_Cell_ID1 : {cellID1}, Bad_Output_Line1 : {this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_01} , Bad_Cell_Judge1 : {this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_01} | Bad_Cell_ID2 : {cellID2}, Bad_Output_Line2 : {this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_RESULT_02} , Bad_Cell_Judge2 : {this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_OUT_DEFECTCD_02}";
                _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.SEALEREQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G4_3_CELL_OUT_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion


        #region Hotpress 관련
        private void __EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE)}] {(value ? "Control" : "Maintenance")} Mode"); // 2024.03.07 HDH : LOG SEALER -> HOTPRESS로 변경
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_03__I_B_ALARM_SET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__I_B_ALARM_SET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF)}] : {this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.HOTPRESSEQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_03__I_W_ALARM_SET_ID.ToString("D6");
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.SET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Set Req No Input Data!!");

                    this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.HOTPRESSEQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HotpressHostBizAlarm(strBizName, this.HOTPRESSEQPID, sender, bizEx, 0);  //$ 2024.08.12 : 세부 Unit으로 Host Alarm 발생 변경

                    this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF)}] : true - {this.BASE.ALARM_RPT_03__I_W_ALARM_SET_ID}");
                    this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF = true;
                }

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT_03__I_B_ALARM_RESET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__I_B_ALARM_RESET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF)}] : {this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF}");
                    return;
                }

                Int32 iRst = 0;
                String strBizName = "BR_EQP_REG_EQPT_ALARM";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_EQP_REG_EQPT_ALARM_IN inData = CBR_EQP_REG_EQPT_ALARM_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.HOTPRESSEQPID;
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT_03__I_W_ALARM_RESET_ID.ToString("D6");

                // RESET시 ALARMID가 0인 경우 EQPT_ALARM_EVENT_TYPE은 값을 Mapping하지 않게 하여 NULL로 인식할 수 있게 하자.
                if (this.BASE.ALARM_RPT_03__I_W_ALARM_RESET_ID != 0)
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.RESET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Reset Req No Input Data!!");

                    this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.HOTPRESSEQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HotpressHostBizAlarm(strBizName, this.HOTPRESSEQPID, sender, bizEx, 0);  //$ 2024.08.12 : 세부 Unit으로 Host Alarm 발생 변경

                    this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF)}] : true - {this.BASE.ALARM_RPT_03__I_W_ALARM_RESET_ID}");
                    this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF = true;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __G3_5_APD_RPT_03__I_B_CELL_EXIST_01_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{nameof(this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_01)} : {value}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __G3_5_APD_RPT_03__I_B_CELL_EXIST_02_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                if (value)
                {
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{nameof(this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_02)} : {value}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __G3_5_APD_RPT_03__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G3_5_APD_RPT_03__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G3_5_APD_RPT_03__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.HOTPRESSEQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.HOTPRESSEQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.HOTPRESSEQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST) // 2024.03.07 HDH : OPERMODE 조건 HOTPRESS로 변경
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT_03__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.MNLMODE, txnID);
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        bool bOccur = RequestNGReply(eventName, this.HOTPRESSEQPID, O_B_REP, O_W_REP_ACK);
                        if (bOccur) return;
                    }
                }

                if (this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_01 == false && this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_02 == false)
                {
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"[{this.HOTPRESSEQPID}] HotPress Cell ID Report : Cell Not Exist");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string cellID1 = IsHR ? this.BASE.G3_5_APD_RPT_03__I_W_CELL_ID_01.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03__I_W_CELL_ID_01);
                string cellID2 = IsHR ? this.BASE.G3_5_APD_RPT_03__I_W_CELL_ID_02.Trim() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03__I_W_CELL_ID_02);
                SolaceLog(this.HOTPRESSEQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_01))} : {cellID1}, {GetDesc(nameof(this.BASE.G4_3_CELL_OUT_RPT_02__I_W_CELL_ID_02))} : {cellID2}");

                int iRst = 0;
                string strBizName = "BR_SET_DEGAS_HOTPRESS_CELL_DATA";
                Exception bizEx = null;

                CBR_SET_DEGAS_HOTPRESS_CELL_DATA_IN inData = CBR_SET_DEGAS_HOTPRESS_CELL_DATA_IN.GetNew(this);
                CBR_SET_DEGAS_HOTPRESS_CELL_DATA_OUT outData = CBR_SET_DEGAS_HOTPRESS_CELL_DATA_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 0;

                if (this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_01)
                {
                    inData.IN_EQP_LENGTH++;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.HOTPRESSEQPID;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SUBLOT_POSITION = "1";
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SUBLOTID = cellID1;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_PORT_NO = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO1).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_TOP_TMPR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP1).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL1).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_TIME = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME1).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_BTM_TMPR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP1.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP1).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].RWK_FLAG = IsHR ? (this.BASE.G3_5_APD_RPT_03__I_B_CELL_REWORK ? "Y" : "N") : (_EIFServer.GetSimValue<bool>(() => this.BASE.G3_5_APD_RPT_03__I_B_CELL_REWORK) ? "Y" : "N");

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"Cell #1 : [Port NO : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO1.ToString()}], [Press 상부 온도 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP1.ToString()}], [Press 하부 온도 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP1.ToString()}]," + $" [Press 하중 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL1.ToString()}], [Press Time : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME1.ToString()}]");
                }

                if (this.BASE.G3_5_APD_RPT_03__I_B_CELL_EXIST_02 == true)
                {
                    inData.IN_EQP_LENGTH++;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = this.HOTPRESSEQPID;
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SUBLOT_POSITION = "2";
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SUBLOTID = cellID2;

                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_PORT_NO = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO2).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_TOP_TMPR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP2).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_WEIGHT_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL2).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_TIME = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME2).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].HOTPRES_BTM_TMPR_VALUE = IsHR ? this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP2.ToString() : _EIFServer.GetSimValue(() => this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP2).ToString();
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].RWK_FLAG = IsHR ? (this.BASE.G3_5_APD_RPT_03__I_B_CELL_REWORK ? "Y" : "N") : (_EIFServer.GetSimValue<bool>(() => this.BASE.G3_5_APD_RPT_03__I_B_CELL_REWORK) ? "Y" : "N");

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"Cell #2 : [Port NO : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PORT_NO2.ToString()}], [Press 상부 온도 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_UPPER_TEMP2.ToString()}], [Press 하부 온도 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_LOWER_TEMP2.ToString()}]," + $" [Press 하중 : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_VAL2.ToString()}], [Press Time : {this.BASE.G3_5_APD_RPT_03_DATA__I_W_PRESS_TIME2.ToString()}]");
                }

                // 입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HotPress Cell BCR Read Req No Input Data!! {this.HOTPRESSEQPID} CellID#1 : {cellID1}, CellID#2 : {cellID2}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //JH 2025.08.04 TK 방지를 위한 Delay 조건 추가 
                AvoidTXRevErr(sender);

                //BizRule Call -> Degas Hot Press 작업 Cell Data 보고 
                iRst = BizCall(strBizName, this.HOTPRESSEQPID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.HOTPRESSEQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.HOTPRESSEQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HotpressHostBizAlarm(strBizName, this.HOTPRESSEQPID, sender, bizEx, 0);

                    HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(this.HOTPRESSEQPID, strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    //HotPress 정보 다운로드
                    HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF} - [CELL ID #1] : {cellID1} - [CELL ID #2] : {cellID2}");
                }
                else
                {
                    ushort uTroublePos = Convert.ToUInt16(outData.OUTDATA[0].TROUBLE_POSITION);

                    //$ 2020.11.02 : Trouble Posion을 빼고 Alarm Type에 해당 내역을 넣는데.. 1,2,3으로 오는 지 확인 필요
                    HotpressHostBizAlarm(strBizName, this.HOTPRESSEQPID, sender, bizEx, uTroublePos, true);

                    HostReply(this.HOTPRESSEQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);

                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF} - [CELL ID #1] : {cellID1} - [CELL ID #2] : {cellID2}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G3_5_APD_RPT_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion
        #endregion
        #endregion

        #region Word Event Method
        #region Common
        //$ 2024.11.13 : Material Change Event 추가
        private void __G1_1_MTRL_MONITER_DATA__I_W_STAT_CHG_EVENT_CODE_OnStringChanged(CVariable sender, string value)
        {
            string strEQPID = this.SEALEREQPID;
            int iRst = -1;
            Exception bizEx;
            string strBizName = "BR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ";

            try
            {
                string strEventCode = "";//this.BASE.G1_1_MTRL_MONITER_DATA__I_W_STAT_CHG_EVENT_CODE;

                if (string.IsNullOrEmpty(strEventCode) || strEventCode == "0") return; //$ 2024.11.13 : Event Code 0을 GMES로 보고 해야 한다면 수정 필요

                CBR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ_IN InData = CBR_EQP_REG_EQPT_WORK_EVENT_EMS_TO_MQ_IN.GetNew(this);

                InData.IN_EQP_LENGTH = 1;
                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                InData.IN_EQP[0].USERID = USERID.EIF;

                InData.IN_EQP[0].EVENTCODE = strEventCode;
                InData.IN_EQP[0].EVENTNAME = string.Empty;
                InData.IN_EQP[0].ACTDTM = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                InData.IN_EQP[0].EQPTID = strEQPID;
                InData.IN_EQP[0].LOTID = string.Empty;

                _EIFServer.SetVarStatusLog(strEQPID, sender, "{0} : Material State Change Report", value);

                //BizRule Call - 재료 교체
                iRst = BizCall(strBizName, this.EQPID, InData, null, out bizEx);
                if (iRst == 0)
                {
                    _EIFServer.SetBizRuleLog(strEQPID, strBizName, InData.Variable, null);
                    _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizName} [RETVAL = > OK]");
                }
                else
                {
                    _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizName} [RETVAL = > NG]");
                    _EIFServer.RegBizRuleException(false, strEQPID, strBizName, string.Empty, InData, bizEx);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(strEQPID, sender, ex);
            }
        }
        #endregion

        #region Unloader
        private void __EQP_STAT_CHG_RPT_01__I_W_EQP_STAT_OnShortChanged(CVariable sender, ushort value)
        {
            int iRst = -1;
            string strBizName = "BR_SET_EQP_STATUS";
            Exception bizEx;

            try
            {
                CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = this.EQPID;

                _EIFServer.SetVarStatusLog(sender, "(EQP State Change)", value);
                _EIFServer.SetVarStatusLog(sender, "(EQP State Change Start)");

                eEqpStatus state = (eEqpStatus)value;

                if (state == eEqpStatus.T)
                {
                    //$ 2020.10.12 : AlarmCode가 기존 String 3Word에서 Int로 변경되어 ToString에 기본 Format을 입력해줌(5자리 00000)
                    //$ 2020.11.10 : 상태가 4로 변경 된 경우에만 Trouble_CD를 입력
                    inData.IN_EQP[0].ALARM_ID = this.BASE.EQP_STAT_CHG_RPT_01__I_W_ALARM_ID.ToString("D6"); //$ 2022.07.21 : Trouble Code 6자리 표준화 D5 -> D6 변경

                    HostBizAlarm(string.Empty, this.EQPID, sender, null, 0, false);

                    //HostConfirmReset();   //$ 2023.08.11 : 불필요한 Logic으로 예상되며, 설비 Timeout을 유발한다고 하여 주석 처리
                }

                //$ 2020.10.12 : 일단 UserStop(8)인 경우보다 큰 경우에는 8로 고정하고 SubStatus를 입력하게 했음, Wait(2)인 경우에도 처리 필요한지 확인 필요
                //$ 2022.10.27 : Wait 상태에서도 SubState가 보고가 필요하여 조건 수정
                if (state >= eEqpStatus.U || state == eEqpStatus.W)
                {
                    inData.IN_EQP[0].EIOSTAT = (state == eEqpStatus.W) ? state.ToString() : eEqpStatus.U.ToString(); //$ 2023.01.18 : 오타 수정

                    if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                    else
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT.ToString();

                    this.PreSubState = this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT;   //$ 2023.05.18 : EQPStatus가 8일 때 SubState 저장하여 SubState Event 처리를 Skip할지 또 할지 결정
                }
                else
                {
                    inData.IN_EQP[0].EIOSTAT = state.ToString();
                    inData.IN_EQP[0].LOSS_CODE = string.Empty;
                    this.PreSubState = 0;   //$ 2023.05.18 : EQPStatus가 8일 아닐 때 초기화
                }

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA_LENGTH > 0 && outData.OUTDATA[0].RETVAL == 0)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{strBizName} [RETVAL = > OK]");
                else
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{strBizName} [RETVAL = > NG]");

                _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{EQPTYPE} State Change End");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT_OnShortChanged(CVariable sender, ushort value)
        {
            string strEQPID = this.EQPID;
            int iRst = -1;
            Exception bizEx;
            string strBizRule = "BR_SET_EQP_STATUS";
            try
            {
                if (value < 8) return;                                            //$ 2023.06.05 : SubState 8이하로 바뀌는 것은 처리 안함.
                if (this.PreSubState == value) return;                            // SubState가 EQPState에서 변경되지 않는 상태(8 이외 상태)이거나 EQPState 8에서 바뀐 값과 같다면 SubState 처리 필요 없음(중복 보고 막음) // HDH 2023.07.14 : PreSubState == 0 조건 삭제
                if (this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_STAT != 8) return;                 // EQPStatus가 8이 아닌 경우 SubState가 없으므로 처리할 필요 없음 

                this.PreSubState = value;

                Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌

                CBR_SET_EQP_STATUS_IN InData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT OutData = CBR_SET_EQP_STATUS_OUT.GetNew(this);

                InData.IN_EQP_LENGTH = 1;

                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                InData.IN_EQP[0].USERID = USERID.EIF;

                InData.IN_EQP[0].EQPTID = strEQPID;

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change)", value);
                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change Start)");

                InData.IN_EQP[0].EIOSTAT = eEqpStatus.U.ToString();

                if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                else
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_SUBSTAT.ToString();

                //BizRule Call
                iRst = BizCall(strBizRule, this.EQPID, InData, OutData, out bizEx);
                if (iRst == 0)
                {
                    _EIFServer.SetBizRuleLog(strEQPID, strBizRule, InData.Variable, OutData.Variable);

                    if (OutData.OUTDATA_LENGTH > 0 && OutData.OUTDATA[0].RETVAL == 0)
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > OK]");
                    }
                    else
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > NG]");
                    }
                }
                else
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizRule, string.Empty, InData, bizEx);
                    //this.HostBizAlarm(sBizRule, sender, bizEx, 0, true); //$ 2020.11.13 : 전엔 HostAlarm 안냈으므로 주석 처리 함(필요시 주석 해제)                 
                }

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change End)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(strEQPID, sender, ex);
            }
        }

        private void __EQP_OP_MODE_CHG_RPT_01__I_W_HMI_LANG_TYPE_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"GP Language #1 : {(value)}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion

        #region Sealer
        private void __EQP_STAT_CHG_RPT_02__I_W_EQP_STAT_OnShortChanged(CVariable sender, ushort value)
        {
            int iRst = -1;
            string strBizName = "BR_SET_EQP_STATUS";
            Exception bizEx;

            try
            {
                CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = this.SEALEREQPID;

                _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, "(EQP State Change)", value);
                _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, "(EQP State Change Start)");

                eEqpStatus state = (eEqpStatus)value;

                if (state == eEqpStatus.T)
                {
                    //$ 2020.10.12 : AlarmCode가 기존 String 3Word에서 Int로 변경되어 ToString에 기본 Format을 입력해줌(5자리 00000)
                    //$ 2020.11.10 : 상태가 4로 변경 된 경우에만 Trouble_CD를 입력
                    inData.IN_EQP[0].ALARM_ID = this.BASE.EQP_STAT_CHG_RPT_02__I_W_ALARM_ID.ToString("D6"); //$ 2022.07.21 : Trouble Code 6자리 표준화 D5 -> D6 변경

                    SealerHostBizAlarm(string.Empty, this.SEALEREQPID, sender, null, 0, false);
                }

                //$ 2020.10.12 : 일단 UserStop(8)인 경우보다 큰 경우에는 8로 고정하고 SubStatus를 입력하게 했음, Wait(2)인 경우에도 처리 필요한지 확인 필요
                //$ 2022.10.27 : Wait 상태에서도 SubState가 보고가 필요하여 조건 수정
                if (state >= eEqpStatus.U || state == eEqpStatus.W)
                {
                    inData.IN_EQP[0].EIOSTAT = (state == eEqpStatus.W) ? state.ToString() : eEqpStatus.U.ToString(); //$ 2023.01.18 : 오타 수정

                    if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                    else
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT.ToString();

                    this.PreSealerSubState = this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT;   //$ 2023.05.18 : EQPStatus가 8일 때 SubState 저장하여 SubState Event 처리를 Skip할지 또 할지 결정
                }
                else
                {
                    inData.IN_EQP[0].EIOSTAT = state.ToString();
                    inData.IN_EQP[0].LOSS_CODE = string.Empty;
                    this.PreSealerSubState = 0;   //$ 2023.05.18 : EQPStatus가 8일 아닐 때 초기화
                }

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.SEALEREQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"HOST {strBizName} - Biz Exception = {iRst}");

                    SealerHostBizAlarm(string.Empty, this.SEALEREQPID, sender, null, 0, false);

                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA_LENGTH > 0 && outData.OUTDATA[0].RETVAL == 0)
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"{strBizName} [RETVAL = > OK]");
                else
                    _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"{strBizName} [RETVAL = > NG]");

                _EIFServer.SetVarStatusLog(this.SEALEREQPID, sender, $"Deagas State Change End");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.SEALEREQPID, sender, ex);
            }
        }

        private void __EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT_OnShortChanged(CVariable sender, ushort value)
        {
            string strEQPID = this.SEALEREQPID;
            int iRst = -1;
            Exception bizEx;
            string strBizRule = "BR_SET_EQP_STATUS";
            try
            {
                if (value < 8) return;                                            //$ 2023.06.05 : SubState 8이하로 바뀌는 것은 처리 안함.
                if (this.PreSealerSubState == value) return;   // SubState가 EQPState에서 변경되지 않는 상태(8 이외 상태)이거나 EQPState 8에서 바뀐 값과 같다면 SubState 처리 필요 없음(중복 보고 막음) // HDH 2023.07.14 : PreSubState == 0 조건 삭제
                if (this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_STAT != 8) return;                 // EQPStatus가 8이 아닌 경우 SubState가 없으므로 처리할 필요 없음 

                this.PreSealerSubState = value;

                Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌

                CBR_SET_EQP_STATUS_IN InData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT OutData = CBR_SET_EQP_STATUS_OUT.GetNew(this);

                InData.IN_EQP_LENGTH = 1;

                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                InData.IN_EQP[0].USERID = USERID.EIF;

                InData.IN_EQP[0].EQPTID = strEQPID;

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Sealer Sub State Change)", value);
                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Sealer Sub State Change Start)");

                InData.IN_EQP[0].EIOSTAT = eEqpStatus.U.ToString();

                if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                else
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_02__I_W_EQP_SUBSTAT.ToString();

                //BizRule Call
                iRst = BizCall(strBizRule, this.EQPID, InData, OutData, out bizEx);
                if (iRst == 0)
                {
                    _EIFServer.SetBizRuleLog(strEQPID, strBizRule, InData.Variable, OutData.Variable);

                    if (OutData.OUTDATA_LENGTH > 0 && OutData.OUTDATA[0].RETVAL == 0)
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > OK]");
                    }
                    else
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > NG]");
                    }
                }
                else
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizRule, string.Empty, InData, bizEx);
                    //this.HostBizAlarm(sBizRule, sender, bizEx, 0, true); //$ 2020.11.13 : 전엔 HostAlarm 안냈으므로 주석 처리 함(필요시 주석 해제)                 
                }

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Sealer Sub State Change End)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(strEQPID, sender, ex);
            }
        }

        private void __EQP_OP_MODE_CHG_RPT_02__I_W_HMI_LANG_TYPE_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"GP Language : {(value)}");

                _EIFServer.SetLanguageID(this.SEALEREQPID, value);  //$ 2023.12.14 : PLC 언어 설정 변경 시 언어 값 저장
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion

        #region HotPress
        private void __EQP_STAT_CHG_RPT_03__I_W_EQP_STAT_OnShortChanged(CVariable sender, ushort value)
        {
            int iRst = -1;
            string strBizName = "BR_SET_EQP_STATUS";
            Exception bizEx;

            try
            {
                CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = this.HOTPRESSEQPID;

                _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, "(EQP State Change)", value);
                _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, "(EQP State Change Start)");

                eEqpStatus state = (eEqpStatus)value;

                if (state == eEqpStatus.T)
                {
                    //$ 2020.10.12 : AlarmCode가 기존 String 3Word에서 Int로 변경되어 ToString에 기본 Format을 입력해줌(5자리 00000)
                    //$ 2020.11.10 : 상태가 4로 변경 된 경우에만 Trouble_CD를 입력
                    inData.IN_EQP[0].ALARM_ID = this.BASE.EQP_STAT_CHG_RPT_03__I_W_ALARM_ID.ToString("D6"); //$ 2022.07.21 : Trouble Code 6자리 표준화 D5 -> D6 변경

                    HotpressHostBizAlarm(string.Empty, this.HOTPRESSEQPID, sender, null, 0, false);
                }

                //$ 2020.10.12 : 일단 UserStop(8)인 경우보다 큰 경우에는 8로 고정하고 SubStatus를 입력하게 했음, Wait(2)인 경우에도 처리 필요한지 확인 필요
                //$ 2022.10.27 : Wait 상태에서도 SubState가 보고가 필요하여 조건 수정
                if (state >= eEqpStatus.U || state == eEqpStatus.W)
                {
                    inData.IN_EQP[0].EIOSTAT = (state == eEqpStatus.W) ? state.ToString() : eEqpStatus.U.ToString(); //$ 2023.01.18 : 오타 수정

                    if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                    else
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT.ToString();

                    this.PreHotPressSubState = this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT;   //$ 2023.05.18 : EQPStatus가 8일 때 SubState 저장하여 SubState Event 처리를 Skip할지 또 할지 결정
                }
                else
                {
                    inData.IN_EQP[0].EIOSTAT = state.ToString();
                    inData.IN_EQP[0].LOSS_CODE = string.Empty;
                    this.PreHotPressSubState = 0;   //$ 2023.05.18 : EQPStatus가 8일 아닐 때 초기화
                }

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.HOTPRESSEQPID, strBizName, string.Empty, inData, bizEx);
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"HOST {strBizName} - Biz Exception = {iRst}");

                    HotpressHostBizAlarm(string.Empty, this.HOTPRESSEQPID, sender, null, 0, false);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                if (outData.OUTDATA_LENGTH > 0 && outData.OUTDATA[0].RETVAL == 0)
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"{strBizName} [RETVAL = > OK]");
                else
                    _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"{strBizName} [RETVAL = > NG]");

                _EIFServer.SetVarStatusLog(this.HOTPRESSEQPID, sender, $"Deagas State Change End");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.HOTPRESSEQPID, sender, ex);
            }
        }

        private void __EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT_OnShortChanged(CVariable sender, ushort value)
        {
            string strEQPID = this.HOTPRESSEQPID;
            int iRst = -1;
            Exception bizEx;
            string strBizRule = "BR_SET_EQP_STATUS";
            try
            {
                if (value < 8) return;                                            //$ 2023.06.05 : SubState 8이하로 바뀌는 것은 처리 안함.
                if (this.PreHotPressSubState == value) return;   // SubState가 EQPState에서 변경되지 않는 상태(8 이외 상태)이거나 EQPState 8에서 바뀐 값과 같다면 SubState 처리 필요 없음(중복 보고 막음) // HDH 2023.07.14 : PreSubState == 0 조건 삭제
                if (this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_STAT != 8) return;                 // EQPStatus가 8이 아닌 경우 SubState가 없으므로 처리할 필요 없음 

                this.PreHotPressSubState = value;

                Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌

                CBR_SET_EQP_STATUS_IN InData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT OutData = CBR_SET_EQP_STATUS_OUT.GetNew(this);

                InData.IN_EQP_LENGTH = 1;

                InData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                InData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                InData.IN_EQP[0].USERID = USERID.EIF;

                InData.IN_EQP[0].EQPTID = strEQPID;

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Hotpress Sub State Change)", value);
                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Hotpress Sub State Change Start)");

                InData.IN_EQP[0].EIOSTAT = eEqpStatus.U.ToString();

                if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                else
                    InData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT_03__I_W_EQP_SUBSTAT.ToString();

                //BizRule Call
                iRst = BizCall(strBizRule, this.EQPID, InData, OutData, out bizEx);
                if (iRst == 0)
                {
                    _EIFServer.SetBizRuleLog(strEQPID, strBizRule, InData.Variable, OutData.Variable);

                    if (OutData.OUTDATA_LENGTH > 0 && OutData.OUTDATA[0].RETVAL == 0)
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > OK]");
                    }
                    else
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizRule} [RETVAL = > NG]");
                    }
                }
                else
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizRule, string.Empty, InData, bizEx);
                    //this.HostBizAlarm(sBizRule, sender, bizEx, 0, true); //$ 2020.11.13 : 전엔 HostAlarm 안냈으므로 주석 처리 함(필요시 주석 해제)                 
                }

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(Hotpress Sub State Change End)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(strEQPID, sender, ex);
            }
        }

        private void __EQP_OP_MODE_CHG_RPT_03__I_W_HMI_LANG_TYPE_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"GP Language : {(value)}");

                _EIFServer.SetLanguageID(this.HOTPRESSEQPID, value);  //$ 2023.12.14 : PLC 언어 설정 변경 시 언어 값 저장
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion
        #endregion

        #region Host Bit Event Method
        // Host Confirm Bit On시 비동기 Thread Method를 통해 10초간 On 유지시 Off(500ms마다 Off 체크, Off 될 경우 Thread 종료)
        public void HOST_CONFIRM_BIT_OnBooleanChanged(CVariable sender, bool value)
        {
            if (value == true)
            {
                Task.Factory.StartNew(() =>
                {
                    if (CVariableAction.TimeOut(Owner[sender.NameCategorized], false, SCANINTERVAL, SECINTERVAL))
                    {
                        this.Owner[sender.NameCategorized].Value = false;
                        _EIFServer.SetVarStatusLog(this.Name, sender, $"Host Bit Off by Host TimeOut({SECINTERVAL}s)"); //$ 2023.03.16 : Host Bit Time Out에 의한 Log 추가
                    }
                });
            }
        }
        #endregion

        #region Remote Event Method
        #region Common
        public virtual void SetRemoteCommand(ushort uCode)
        {
            switch (uCode)
            {
                case 1:  //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND_01__O_W_REMOTE_COMMAND_CODE = uCode;
                    this.BASE.REMOTE_COMM_SND_01__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Unloader
        //$ 2020.11.10 : UI에서 Remote Command를 받아야 하는 구조인데.. UI에서 줄수 있을지 없을지 몰라 일단 가상 변수로 처리 함
        private void __REMOTE_COMM_SND_01__V_REMOTE_CMD_OnShortChanged(CVariable sender, ushort value)
        {
            switch (value)
            {
                case 1: //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND_01__O_W_REMOTE_COMMAND_CODE = value;
                    this.BASE.REMOTE_COMM_SND_01__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }

        //$ 2020.11.10 : EIF에 요청한 Remote Command에 대한 Confirm 처리
        private void __REMOTE_COMM_SND_01__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{sender.Description} : ", value);

                if (value)
                {
                    string sLog = $"REMOTE_COMM_SND_01__I_B_REMOTE_COMMAND_CONF : {value}, REMOTE_COMM_SND_01__I_W_REMOTE_COMMAND_CONF_ACK : {this.BASE.REMOTE_COMM_SND_01__I_W_REMOTE_COMMAND_CONF_ACK}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    this.BASE.REMOTE_COMM_SND_01__O_W_REMOTE_COMMAND_CODE = 0;
                    this.BASE.REMOTE_COMM_SND_01__O_B_REMOTE_COMMAND_SEND = false;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion

        #region Sealer
        //$ 2020.11.10 : UI에서 Remote Command를 받아야 하는 구조인데.. UI에서 줄수 있을지 없을지 몰라 일단 가상 변수로 처리 함
        private void __REMOTE_COMM_SND_02__V_REMOTE_CMD_OnShortChanged(CVariable sender, ushort value)
        {
            switch (value)
            {
                case 1: //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND_02__O_W_REMOTE_COMMAND_CODE = value;
                    this.BASE.REMOTE_COMM_SND_02__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }

        //$ 2020.11.10 : EIF에 요청한 Remote Command에 대한 Confirm 처리
        private void __REMOTE_COMM_SND_02__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{sender.Description} : ", value);

                if (value)
                {
                    string sLog = $"REMOTE_COMM_SND_02__I_B_REMOTE_COMMAND_CONF : {value}, REMOTE_COMM_SND_02__I_W_REMOTE_COMMAND_CONF_ACK : {this.BASE.REMOTE_COMM_SND_02__I_W_REMOTE_COMMAND_CONF_ACK}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    this.BASE.REMOTE_COMM_SND_02__O_W_REMOTE_COMMAND_CODE = 0;
                    this.BASE.REMOTE_COMM_SND_02__O_B_REMOTE_COMMAND_SEND = false;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion

        #region HotPress
        //$ 2020.11.10 : UI에서 Remote Command를 받아야 하는 구조인데.. UI에서 줄수 있을지 없을지 몰라 일단 가상 변수로 처리 함
        private void __REMOTE_COMM_SND_03__V_REMOTE_CMD_OnShortChanged(CVariable sender, ushort value)
        {
            switch (value)
            {
                case 1: //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND_03__O_W_REMOTE_COMMAND_CODE = value;
                    this.BASE.REMOTE_COMM_SND_03__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }

        //$ 2020.11.10 : EIF에 요청한 Remote Command에 대한 Confirm 처리
        private void __REMOTE_COMM_SND_03__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{sender.Description} : ", value);

                if (value)
                {
                    string sLog = $"REMOTE_COMM_SND_03__I_B_REMOTE_COMMAND_CONF : {value}, REMOTE_COMM_SND_03__I_W_REMOTE_COMMAND_CONF_ACK : {this.BASE.REMOTE_COMM_SND_03__I_W_REMOTE_COMMAND_CONF_ACK}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    this.BASE.REMOTE_COMM_SND_03__O_W_REMOTE_COMMAND_CODE = 0;
                    this.BASE.REMOTE_COMM_SND_03__O_B_REMOTE_COMMAND_SEND = false;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }
        #endregion
        #endregion
        #endregion

        #region TACTTIME COLLECT
        private void TacttimeReport()
        {
            try
            {
                //Wait(this.BASE.EQP_INFO__V_TACTTIME_INTERVAL * 1000); //$ 2025.09.22 : Wait를 Scheduler 함수 내부에서 호출 시 해당 시간 만큼 무중단 패치 시 Delay걸려 주석 처리

                EIFMonitoringData();

                if (!IsRl) return; //$ 2023.02.06 : 시뮬레이션 모드 인 경우 Tact Time 보고 안함(사전 검수 등 Biz 없는데 Test할 필요가 없음)

                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE; //$ 2024.09.05 : Source Compare하면서 누락 내역 추가

                #region tacttime data collet    
                Exception bizEx;

                if (this.TactEQPIDs == null)
                {
                    _EIFServer.SetLog(this.EQPID, $"Tact EQPID Not Exist - Biz Report Fail");
                    return;
                }

                for (int i = 0; i < this.TactEQPIDs.Length; i++)
                {
                    if (string.IsNullOrEmpty(this.TactEQPIDs[i])) continue;

                    string unitID = this.TactEQPIDs[i];

                    ushort tactTime = this.BASE.Variables[$"EQP_STAT_CHG_RPT_{i + 1:D2}:I_W_EQP_TACT_TIME"].AsShort;

                    //$ 2022.09.30 : Tact 관련 정보가 0이라도 0으로 보고 할 수 있도록 하기 조건 주석 처리
                    //if (tactTime == 0)
                    //{
                    //    this.SetLog(eqpIDs[i], "TACT_TIME = 0 Biz Report Pass");
                    //    continue;
                    //}

                    eEqpStatus state = (eEqpStatus)this.BASE.Variables[$"EQP_STAT_CHG_RPT_{i + 1:D2}:I_W_EQP_STAT"].AsShort;

                    CBR_EQP_REG_EQPT_OPER_INFO_IN inData = CBR_EQP_REG_EQPT_OPER_INFO_IN.GetNew(this);
                    inData.IN_EQP_LENGTH = 1;

                    inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                    inData.IN_EQP[0].TACT_TIME = (state == eEqpStatus.R) ? tactTime : 0; //JH 2024.04.24 김인기 팀장님요청 : 설비 Run 상태를 제외한 나머지 상태에는 Tact Time 0 보고해야함 
                    inData.IN_EQP[0].CELLID = string.Empty;
                    inData.IN_EQP[0].EQPTID = unitID;
                    inData.IN_EQP[0].DAYNIGHT_TYPE_CODE = "1";

                    //BizRule Call
                    // 하기 Biz에 Degas는 Indata를 여러개 처리 가능한데.. EOL은 Indata가 복수일 경우 Exception이 발생하여 Biz 개별 호출하도록 변경함
                    int iRst = BizCall("BR_EQP_REG_EQPT_OPER_INFO", unitID, inData, null, out bizEx);
                    if (iRst != 0)
                        _EIFServer.RegBizRuleException(SIMULATION_MODE, unitID, "BR_EQP_REG_EQPT_OPER_INFO", string.Empty, inData, bizEx);

                    if (bLogging) _EIFServer.SetLog(unitID, $"TACT_TIME = {tactTime} Biz Report Success"); //$ 2024.09.05 : Source Compare하면서 누락 내역 추가

                    Wait(500);
                }
                #endregion                
            }
            catch (Exception ex)
            {
                _EIFServer.SetLog($"TACTTIME COLLECT Exception : {ex.ToString()}");
            }
        }
        #endregion


        #region ETC Method
        #region Common
        private Int16 Get_Tray_Type_Cnt1(CVariable sender, string sTrayID)
        {
            int iRst = 0;
            string strBizName = "BR_GET_TRAYID_VALIDATION";
            Exception bizEx = null;
            short shCellCnt = -1;
            try
            {
                CBR_GET_TRAYID_VALIDATION_IN inData = CBR_GET_TRAYID_VALIDATION_IN.GetNew(this);
                CBR_GET_TRAYID_VALIDATION_OUT outData = CBR_GET_TRAYID_VALIDATION_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = sTrayID;
                inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;

                // 입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetStatusLog($"GET_TRAYID_VALIDATION No Input Data!! {this.EQPID} {this.BASE.G2_2_CARR_IN_RPT__I_W_TRAY_ID}");
                    return shCellCnt;
                }

                //BizRule Call
                iRst = BizCall(strBizName, this.EQPID, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 1);

                    return shCellCnt;
                }

                shCellCnt = Convert.ToInt16(outData.TB_TRAY_TYPE[0].CST_CELL_QTY);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }

            return shCellCnt;
        }

        // Host Confirm 초기화 메소드
        private void HostConfirmReset()
        {
            if (this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G2_2_CARR_IN_RPT__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G2_2_CARR_IN_RPT__O_B_TRIGGER_REPORT_CONF = false;
            }

            if (this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G2_3_CARR_OUT_RPT__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF = false;
            }

            if (this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF = false;
            }

            //$ 2023.02.01 : 누락되어 추가
            this.BASE.HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND = false;

            //$ 2023.02.01 : 신규 설비 Alarm 대응 추가
            this.BASE.ALARM_RPT_01__O_B_ALARM_SET_CONF = false;
            this.BASE.ALARM_RPT_01__O_B_ALARM_RESET_CONF = false;


            if (this.BASE.G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G4_3_CELL_OUT_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G4_3_CELL_OUT_RPT_01__O_B_TRIGGER_REPORT_CONF = false;
            }

            //$ 2023.01.27 : 누락된 Host Bit 초기화 추가
            if (this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G3_5_APD_RPT_01__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G3_5_APD_RPT_01__O_B_TRIGGER_REPORT_CONF = false;
            }

            if (this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G3_5_APD_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G3_5_APD_RPT_02__O_B_TRIGGER_REPORT_CONF = false;
            }

            if (this.BASE.G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G4_3_CELL_OUT_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G4_3_CELL_OUT_RPT_02__O_B_TRIGGER_REPORT_CONF = false;
            }

            if (this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF)
            {
                this.BASE.G3_5_APD_RPT_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G3_5_APD_RPT_03__O_B_TRIGGER_REPORT_CONF = false;
            }

            //$ 2023.02.01 : 누락되어 추가
            this.BASE.HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND = false;
            this.BASE.HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND = false;

            //$ 2023.02.01 : 신규 설비 Alarm 대응 추가
            this.BASE.ALARM_RPT_02__O_B_ALARM_SET_CONF = false;
            this.BASE.ALARM_RPT_02__O_B_ALARM_RESET_CONF = false;

            //$ 2023.02.01 : 신규 설비 Alarm 대응 추가
            this.BASE.ALARM_RPT_03__O_B_ALARM_SET_CONF = false;
            this.BASE.ALARM_RPT_03__O_B_ALARM_RESET_CONF = false;
        }

        //$ 2024.11.22 : SolaceBiz 실행 Log Flag 인자 추가, EIF Log 및 Biz Log는 해당 인자에 맞게 처리
        //$ 2024.10.16 : SolaceBiz 실행 Log Flag 인자 추가, EIF Log 및 Biz Log는 해당 인자에 맞게 처리
        public int BizCall(string bizName, string eqpID, CStructureVariable inVariable, CStructureVariable outVariable, out Exception bizEx, string txnID = "", bool bLogging = true)
        {
            bizEx = null;
            DateTime preTime = DateTime.Now;

            //$ 2025.05.14 : Solace Log는 tnxID가 없는 경우 남기지 않음
            if (string.IsNullOrEmpty(txnID))
            {
                _EIFServer.EnableLoggingBizRule = bLogging;

                int nRst = (SIMULATION_MODE) ? 0 : _EIFServer.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BizTimeout, out bizEx);

                _EIFServer.EnableLoggingBizRule = true;

                return nRst;
            }
            ;

            SolaceLog(eqpID, txnID, 3, $"{bizName} : {inVariable.Variable.ToString()}");

            SolaceLog(eqpID, txnID, 4, $"{bizName}"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

            _EIFServer.EnableLoggingBizRule = bLogging;

            int iRst = (SIMULATION_MODE) ? 0 : _EIFServer.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BizTimeout, out bizEx);

            _EIFServer.EnableLoggingBizRule = true;

            SolaceLog(eqpID, txnID, 4, $"{bizName} [{(DateTime.Now - preTime).TotalMilliseconds:0.0}ms] - {iRst}");
            //if (bLogging) _EIFServer.SetLog("BIZ", $"{bizName} - End {iRst}, [{(DateTime.Now - preTime).TotalSeconds:0.000}s]");

            if (iRst == 0 && outVariable != null)
                SolaceLog(eqpID, txnID, 5, $"{bizName} : {outVariable.Variable.ToString()}");

            return iRst;
        }

        //$ 2023.03.24 : 원활한 설비 테스트를 위해 NAK Test와 TimeOut Test를 동시에 선택한 경우 Nak한번 Timeout한번 발생 후 정상진행하게 하여 다음 Event 테스트하게 함(Test 개별 선택시에는 기존과 동일)
        private bool RequestNGReply(string methodName, string eqpID, string boolProperty, string shortProperty)
        {
            if (this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
            {
                if (this.NakPassList == null) this.NakPassList = new Dictionary<string, bool>();
                if (this.TimeOutPassList == null) this.TimeOutPassList = new Dictionary<string, bool>();

                if (!this.NakPassList.ContainsKey(methodName) || (this.NakPassList.ContainsKey(methodName) && this.NakPassList[methodName] == false))
                {
                    _EIFServer.SetNakData(eqpID, boolProperty, shortProperty);
                    this.NakPassList.Add(methodName, true);
                    return true;
                }

                if (!this.TimeOutPassList.ContainsKey(methodName) || (this.TimeOutPassList.ContainsKey(methodName) && this.TimeOutPassList[methodName] == false))
                {
                    this.TimeOutPassList.Add(methodName, true);
                    return true;
                }

                return false;
            }
            else if (this.BASE.TESTMODE__V_IS_NAK_TEST)
            {
                _EIFServer.SetNakData(eqpID, boolProperty, shortProperty);
                return true;
            }
            else if (this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Unloader
        private void HostBizAlarm(string strBizName, string eqpID, CVariable sender, Exception bizEx, ushort uType, bool bFlag = true)
        {
            int iTroubleCD = 0;
            string sTroubleCD = string.Empty;
            string sMessage = string.Empty;
            try
            {
                if (bFlag)
                {
                    _EIFServer.BizException(SIMULATION_MODE, strBizName, eqpID, bizEx, out sTroubleCD, out sMessage);
                    sMessage = $"{uType}_{sMessage}";
                }

                //$ 2023.01.03 : Biz Trouble Code가 숫자 변환이 안되는 경우 처리, 현재 6만번때 Host Trouble이 없어 6만을 Default값으로 사용
                if (!int.TryParse(sTroubleCD, out iTroubleCD))
                    iTroubleCD = EIFALMCD.DEFAULT;

                HostAlarm(sender, iTroubleCD, sMessage, uType, bFlag);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
            }
        }

        private void HostAlarm(CVariable sender, int iTroubleCD, string sMessage, ushort uType, bool bFlag = true)
        {
            //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
            if (bFlag)
            {

                List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_MSG = hostMsg;
                this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_SEND_SYSTEM = 0;
                this.BASE.HOST_ALARM_MSG_SEND_01__O_W_EQP_PROC_STOP_TYPE = 0;

                this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_ID = iTroubleCD;
                this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_DISP_TYPE = uType;
            }

            this.BASE.HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND = bFlag;

            string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
            if (bFlag)
                sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

            _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
        }

        private void HostAlarm(int iTroubleCD, string sMessage, ushort uType, bool bFlag, ushort stopType, ushort action)
        {
            lock (_lockHostAlm_01)
            {
                //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
                if (bFlag)
                {
                    List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                    this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_MSG = hostMsg;
                    this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_ID = iTroubleCD;
                    this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_DISP_TYPE = uType;

                    this.BASE.HOST_ALARM_MSG_SEND_01__O_W_HOST_ALARM_SEND_SYSTEM = (ushort)stopType;
                    this.BASE.HOST_ALARM_MSG_SEND_01__O_W_EQP_PROC_STOP_TYPE = (ushort)action;
                }

                this.BASE.HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND = bFlag;

                string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_01__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
                if (bFlag)
                    sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

                _EIFServer.SetStatusLog(this.Name, this.EQPID, sLog);
            }
        }
        #endregion

        #region Sealer
        private void SealerHostBizAlarm(string strBizName, string eqpID, CVariable sender, Exception bizEx, ushort uType, bool bFlag = true)
        {
            int iTroubleCD = 0;
            string sTroubleCD = string.Empty;
            string sMessage = string.Empty;
            try
            {
                if (bFlag)
                {
                    _EIFServer.BizException(SIMULATION_MODE, strBizName, eqpID, bizEx, out sTroubleCD, out sMessage);
                    sMessage = $"{uType}_{sMessage}";
                }

                //$ 2023.01.03 : Biz Trouble Code가 숫자 변환이 안되는 경우 처리, 현재 6만번때 Host Trouble이 없어 6만을 Default값으로 사용
                if (!int.TryParse(sTroubleCD, out iTroubleCD))
                    iTroubleCD = EIFALMCD.DEFAULT;

                SealerHostAlarm(sender, iTroubleCD, sMessage, uType, bFlag);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
            }
        }

        private void SealerHostAlarm(CVariable sender, int iTroubleCD, string sMessage, ushort uType, bool bFlag = true)
        {
            //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
            if (bFlag)
            {
                List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_MSG = hostMsg;
                this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_SEND_SYSTEM = 0;
                this.BASE.HOST_ALARM_MSG_SEND_02__O_W_EQP_PROC_STOP_TYPE = 0;

                this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_ID = iTroubleCD;
                this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_DISP_TYPE = uType;
            }

            this.BASE.HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND = bFlag;

            string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
            if (bFlag)
                sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

            _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
        }

        //JH 2025.06.05 타 시스템(FDC, SPC etc..)에서 요청하여 발생하는 SSFHostAlarm
        private void SealerHostAlarm(int iTroubleCD, string sMessage, ushort uType, bool bFlag, ushort stopType, ushort action)
        {
            lock (_lockHostAlm_02)
            {
                //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
                if (bFlag)
                {
                    List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                    this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_MSG = hostMsg;
                    this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_ID = iTroubleCD;
                    this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_DISP_TYPE = uType;

                    this.BASE.HOST_ALARM_MSG_SEND_02__O_W_HOST_ALARM_SEND_SYSTEM = stopType;
                    this.BASE.HOST_ALARM_MSG_SEND_02__O_W_EQP_PROC_STOP_TYPE = action;
                }

                this.BASE.HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND = bFlag;

                string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_02__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
                if (bFlag)
                    sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

                _EIFServer.SetStatusLog(this.Name, this.EQPID, sLog);
            }
        }
        #endregion

        #region Hotpress
        private void HotpressHostBizAlarm(string strBizName, string eqpID, CVariable sender, Exception bizEx, ushort uType, bool bFlag = true)
        {
            int iTroubleCD = 0;
            string sTroubleCD = string.Empty;
            string sMessage = string.Empty;
            try
            {
                if (bFlag)
                {
                    _EIFServer.BizException(SIMULATION_MODE, strBizName, eqpID, bizEx, out sTroubleCD, out sMessage);
                    sMessage = $"{uType}_{sMessage}";
                }

                //$ 2023.01.03 : Biz Trouble Code가 숫자 변환이 안되는 경우 처리, 현재 6만번때 Host Trouble이 없어 6만을 Default값으로 사용
                if (!int.TryParse(sTroubleCD, out iTroubleCD))
                    iTroubleCD = EIFALMCD.DEFAULT;

                HotpressHostAlarm(sender, iTroubleCD, sMessage, uType, bFlag);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
            }
        }

        private void HotpressHostAlarm(CVariable sender, int iTroubleCD, string sMessage, ushort uType, bool bFlag = true)
        {
            //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
            if (bFlag)
            {
                List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_MSG = hostMsg;
                this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_SEND_SYSTEM = 0;
                this.BASE.HOST_ALARM_MSG_SEND_03__O_W_EQP_PROC_STOP_TYPE = 0;

                this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_ID = iTroubleCD;
                this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_DISP_TYPE = uType;
            }

            this.BASE.HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND = bFlag;

            string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
            if (bFlag)
                sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

            _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
        }

        //JH 2025.06.05 타 시스템(FDC, SPC etc..)에서 요청하여 발생하는 SSFHostAlarm
        private void HotpressHostAlarm(int iTroubleCD, string sMessage, ushort uType, bool bFlag, ushort stopType, ushort action)
        {
            lock (_lockHostAlm_03)
            {
                //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
                if (bFlag)
                {
                    List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                    this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_MSG = hostMsg;
                    this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_ID = iTroubleCD;
                    this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_DISP_TYPE = uType;

                    this.BASE.HOST_ALARM_MSG_SEND_03__O_W_HOST_ALARM_SEND_SYSTEM = stopType;
                    this.BASE.HOST_ALARM_MSG_SEND_03__O_W_EQP_PROC_STOP_TYPE = action;
                }

                this.BASE.HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND = bFlag;

                string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND_03__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
                if (bFlag)
                    sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

                _EIFServer.SetStatusLog(this.Name, this.EQPID, sLog);
            }
        }
        #endregion

        #region Solace Log Method //$ 2025.05.14 : 전극/조립쪽과 같은 I/O 명으로 Log를 남기기 위해 Logic 반영
        private void HostReply(string eqpID, string boolProperty, bool bitFlag, string wordProperty = null, eConfirm ackCode = eConfirm.DEFAULT, string txnID = "")
        {
            if (!string.IsNullOrEmpty(wordProperty)) //$ 2024.02.16 : short Property가 Empty일 경우 Skip하도록 수정(boolProperty로 조건 걸려있어 Exception 발생)
            {
                var propShort = this.BASE.GetType().GetProperty(wordProperty);
                propShort.SetValue(this.BASE, (ushort)ackCode);

                if (bitFlag) //Bit Off에 대해서는 Word 변경 내역을 보고 하지 않는다고 하여 조건 처리
                    SolaceLog(eqpID, txnID, 6, $"{GetDesc(wordProperty)}_{(ushort)ackCode}");
            }

            var itemBool = this.BASE.GetType().GetProperty(boolProperty);
            itemBool.SetValue(this.BASE, bitFlag);

            if (bitFlag)
                SolaceLog(eqpID, txnID, 7, $"{GetDesc(boolProperty)} : On");
            else
                SolaceLog(eqpID, txnID, 9, $"{GetDesc(boolProperty)} : Off");
        }

        public void SolaceLog(string eqpID, string txnID, int iStepNo, string message)
        {
            _EIFServer.SetSolLog(eqpID, txnID, iStepNo, message); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
        }

        public string GetDesc(string propertyName)  //$ 2025.05.14 : 동일한 Address에 대해 Event를 구분하기 위해 idx 추가
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                string key = propertyName;
                if (this.PropertyDesc.ContainsKey(key))
                {
                    return this.PropertyDesc[key]; //Desc에서 값을 한번이라도 읽어오면, 내부 Dictionary에서 찾게 하자..
                }
                else
                {
                    var reflectProperty = this.BASE.GetType().GetProperty("__" + propertyName, typeof(CVariable));

                    var item = (CVariable)reflectProperty.GetValue(this.BASE, null);

                    this.PropertyDesc.Add(key, item.NameCategorized);

                    return item.NameCategorized;
                }
            }

            return "";
        }

        //$ 2025.08.13 : TxnID를 저장해 두고 Event Off시 연속하여 사용
        private void SetTxnID(string eventName, string txnID)
        {
            if (!this.EventTxnID.ContainsKey(eventName))
                this.EventTxnID.Add(eventName, txnID);
            else
                this.EventTxnID[eventName] = txnID;
        }

        //$ 2025.08.13 : 저장해 둔 TxnID를 추출하여 Solace Log를 연계하게 하고 저장값 초기화, 저장 값이 없다면 
        private string GetTxnID(string eventName, string txnID)
        {
            if (!this.EventTxnID.ContainsKey(eventName)) return txnID; //이전 Key(ID)가 없으면 최근 발행된 Key 사용

            string preKey = this.EventTxnID[eventName];
            this.EventTxnID[eventName] = string.Empty; //이전 Key(ID)를 추출했으면 Value 초기화

            return preKey;
        }
        #endregion

        #region Mointoring
        private void EIFMonitoringData()
        {
            try
            {
                this.MONITOR__V_MONITOR_SOLACE = _EIFServer.ConnectionState.ToString();
                if (_EIFServer.Drivers.Count >= 1)
                {
                    if (_EIFServer.Drivers[1].ConnectionState == enumConnectionState.Connected)
                        this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.ONLINE.ToString();
                    else
                        this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.OFFLINE.ToString();
                }

                this.MONITOR__V_MONITOR_EQPSTATUS = GetStringEqpStat(this.BASE.EQP_STAT_CHG_RPT_01__I_W_EQP_STAT);
            }
            catch (Exception ex)
            {
                _EIFServer.SetLog($"EIFMonitoringData Exception : {ex.ToString()}");
            }
        }

        private string GetStringEqpStat(ushort eqpState)
        {
            string result = string.Empty;

            switch (eqpState)
            {
                case 0:
                    result = "Power Off";
                    break;
                case 1:
                    result = "Run";
                    break;
                case 2:
                    result = "Wait";
                    break;
                case 4:
                    result = "Trouble";
                    break;
                case 8:
                    result = "User Stop";
                    break;
            }

            return result;
        }
        #endregion

        #region //JH 2025.08.04 TK 역전 현상을 피하기 위한 BizCall Delay 관련 메소드 추가
        private void AvoidTXRevErr(CVariable sender)
        {
            short retryCnt = 0;

            while (this.BASE.DEG_LD_G2_3_CARR_OUT_RPT__I_B_TRIGGER_REPORT && !this.BASE.DEG_LD_G2_3_CARR_OUT_RPT__O_B_TRIGGER_REPORT_CONF && retryCnt <= this.DEG_LD__V_W_MAX_RETRY_CNT) //무한 루프를 돌지 않기 위해 조건문 추가
            {
                retryCnt++;
                //this._EIFServer.SetVarStatusWarnLog(this.Name, sender, $"Wait to avoid Transaction Error - retry count : {retryCnt}");
                Wait(this.DEG_LD__V_W_WAITTIME); //wait 타임을 가상변수로 생성하여 Setting
            }

            if (retryCnt > 0) this._EIFServer.SetAvoidTXRevErrLog(this.Name, sender, $"Wait to avoid Transaction Error - retry count : {retryCnt}");
        }
        #endregion
        #endregion
    }
}