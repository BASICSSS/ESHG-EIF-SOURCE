using System;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;
using LGCNS.ezControl.EIF.Solace;
using LGCNS.ezControl.Data;

using SolaceSystems.Solclient.Messaging;
using Newtonsoft.Json;

using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.EOLVISION
{
    public partial class CEOLVISION_BIZ : CImplement, IEIF_Biz
    {
        #region Class Member variable
        public const string EQPTYPE = "EOLVISION";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!

        private short SCANINTERVAL = 500; //msec
        private short SECINTERVAL = 10;    //sec

        #region Simulation Mode 설정 관련
        public const Boolean SIMULATION_MODE = false; //$ 2021.07.12 : 사전 검수 모드는 빌드를 통해서만 바꿀수 있게 하자
        protected bool IsRl { get { return !SIMULATION_MODE; } } //IsReal을 FullName로 쓰면 너무길어서 약어로 쓴다. 알아봐야 할텐데..
        #endregion

        #region Host Simulation Mode 설정 관련
        private const Boolean HOST_SIMULATION_MODE = false; //$ 2021.11.24 : GMES 통합 테스트 모드는 빌드를 통해서만 바꿀수 있게 하자
        protected bool IsHR { get { return !HOST_SIMULATION_MODE; } } //IsHostReal을 FullName로 쓰면 너무길어서 약어로 쓴다. 알아봐야 할텐데..
        #endregion

        public string ReqQueue { get { return this.BIZ_INFO__V_REQQUEUE_NAME; } }
        public string RepQueue { get { return $"REPLY/{this.BIZ_INFO__V_REQQUEUE_NAME}"; } }

        public int BizTimeout { get { return this.BIZ_INFO__V_BIZCALL_TIMEOUT; } }

        public String EifFileName => $"{this.Name}{"_EIF"}";

        private CEOLVISION BASE { get { return (Owner as CEOLVISION); } }

        public string EQPID { get { return this.BASE.EQP_INFO__V_W_EQP_ID; } }

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

        private ushort PreSubState { get; set; } //$ 2023.05.18 : EQPState가 8로 변경 될 때 SubState 저장, 나머지 상태에서는 0으로 입력 됨

        private CCommon m_Common;

        //$ 중요 : Modeler에 등록 된 Control Server의 이름으로 Key를 써야 하는데.. 귀찮으니 첫 번째 값을 쓰도록 하자. Base는 HPCD, Imp는 HPCDImp 이므로 무조건 First가 Base
        protected override CElement Owner => CExecutor.ElementsByElementPath.First().Value;  //CExecutor.ElementsByElementPath["ESHG.MEL.GAHPCD2"]; 
        private CSolaceEIFServerBizRule _EIFServer = null;

        private Dictionary<string, bool> NakPassList = null;
        private Dictionary<string, bool> TimeOutPassList = null;
        //JH 2025.06.09 현재 사양 상 Vision에서 APD 보고하지 않음
        private Dictionary<int, string> ApdItemNameLists = new Dictionary<int, string>();           //$ 2021.10.28 : CLCTITEM_NO를 Key로 CLCTITEM을 Value로 저장할 Dictionary
        private Dictionary<int, int> ApdItemFPointLists = new Dictionary<int, int>();               //$ 2021.10.28 : CLCTITEM_NO를 Key로 FPOINT를 Value로 저장할 Dictionary

        private List<string> BizApdItemNameLists = new List<string>();
        private Dictionary<string, List<string>> BizApdItemValuesLists = new Dictionary<string, List<string>>();
        private Dictionary<string, string> BizApdJudgeLists = new Dictionary<string, string>();

        private Dictionary<string, string> PropertyDesc = null;
        private Dictionary<string, string> EventTxnID = null; //$ 2025.08.13 : TxnID를 저장할 Dictionary

        private object _lockHostAlm = new object();
        #endregion

        #region FactovaLync Method Override

        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            __INTERNAL_VARIABLE_STRING("V_W_LANE_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP LANE ID");
            __INTERNAL_VARIABLE_STRING("V_W_EQP_KIND_CD", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP KIND CD");
            __INTERNAL_VARIABLE_SHORT("V_CELL_CLCT_DATA_CNT", "ADD_INFO", enumAccessType.Virtual, 0, 0, false, true, 0, string.Empty, "APD List Count");
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_SIXLOSSCODE_USE", "EQP_INFO", enumAccessType.Virtual, true, false, false, "", "True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용"); //$ 2023.07.26 : Loss Code 3자리 or 6자리 사용 여부

            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "BIZ_INFO", enumAccessType.Virtual, false, true, "", "", "EIF -> Biz Server Req Queue Name");
            __INTERNAL_VARIABLE_INTEGER("V_BIZCALL_TIMEOUT", "BIZ_INFO", enumAccessType.Virtual, 0, 0, true, false, 30000, string.Empty, "Biz Call TimeOut(mSec)");

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
            _EIFServer.HandleEmptyStringByNull = true; //$ 2024.11.11 : Biz Mapping안하면 Null로 보고

            //$ 2023.01.19 : Host Bit 초기화 시점을 Onstarted에서 OnInitializeCompleted로 변경
            _EIFServer.SetStatusLog("Factova Initialize Completed");
            _EIFServer.SetSolaceInfo(this.ReqQueue, this.RepQueue, this.BizTimeout); //$ 2024.11.17 : Solace 관련 정보를 Common으로 사전 입력

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

            //LoadCLCTItems();  //JH 2025.06.09 현재 사양 상 Vision에서 APD 보고하지 않음

            _EIFServer.SetStatusLog("EIF FactoryLync(L2) Started");
            m_Common = new CCommon(_EIFServer, this, SIMULATION_MODE);
            this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !this.BASE.HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF;

            _EIFServer.SetLanguageID(this.EQPID, this.BASE.EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE);  //$ 2023.12.14 : 프로그램 시작 시 PLC 사용 언어 설정

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
            #endregion

            #region EOLVISION
            __EVENT_BOOLEANCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE, __EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT__I_B_ALARM_SET_REQ, __ALARM_RPT__I_B_ALARM_SET_REQ_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT__I_B_ALARM_RESET_REQ, __ALARM_RPT__I_B_ALARM_RESET_REQ_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT, __G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT_OnBooleanChanged);

            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT__I_W_EQP_STAT, __EQP_STAT_CHG_RPT__I_W_EQP_STAT_OnShortChanged, true);
            __EVENT_SHORTCHANGED(this.BASE.__EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT, __EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT_OnShortChanged, true); //$ 2023.05.18 : SubState 변경 대응
            __EVENT_SHORTCHANGED(this.BASE.__EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE, __EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE_OnShortChanged, true);
            #endregion
            #endregion

            #region HOST Area
            #region Common
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT__O_B_ALARM_SET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__ALARM_RPT__O_B_ALARM_RESET_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion

            #region EOLVISION
            __EVENT_BOOLEANCHANGED(this.BASE.__G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF, HOST_CONFIRM_BIT_OnBooleanChanged);

            __EVENT_BOOLEANCHANGED(this.BASE.__HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);
            #endregion            
            #endregion

            #region Remote Command
            __EVENT_SHORTCHANGED(this.BASE.__REMOTE_COMM_SND__V_REMOTE_CMD, __REMOTE_COMM_SND__V_REMOTE_CMD_OnShortChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND, HOST_CONFIRM_BIT_OnBooleanChanged);
            __EVENT_BOOLEANCHANGED(this.BASE.__REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF, __REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged);
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

        private void CGEOLVision_OnBooleanChanged(CVariable sender, bool value)
        {
            //this.Elements["RMS"].Variables["RMS_INFO:V_W_RMS_IS_CONNECTED"].AsBoolean = value; //JH 2025.06.09 현재 Vision은  RMS 사용안함
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
                if (this.EQPID != msg.refDS.IN_DATA[0].EQPTID)
                {
                    _EIFServer.SetWarnLog($"[RCVDMSG] [Received EQPID is not valid.]  Message: {message}", EifFileName, this.EQPID);
                    return;
                }

                // HMI Language Type 에 따른 Messge
                string rcvdAlarmMsg = string.Empty;
                switch (this.BASE.EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE)
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

                // Send Alarm
                HostAlarm(EIFALMCD.DEFAULT, rcvdAlarmMsg, 1, true, sendSystem, stop);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.EQPID, this.EQPID);
            }
        }
        #endregion

        #region Bit Event Method
        #region Common
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
                //this.BASE.COMM__O_B_COMM_CHECK = !this.BASE.COMM__I_B_COMM_CHECK_CONF;
                this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !this.BASE.HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF;
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __HOST_COMM_CHK__I_B_HOST_COMM_CHK_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                //this.BASE.COMM__O_B_COMM_CHECK = !value;
                this.BASE.HOST_COMM_CHK__O_B_HOST_COMM_CHK = !value;
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.Name, sender, ex);
            }
        }

        private void __DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ_OnVariableOn(CVariable sender)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (Host - EQP System Time Sync Bit On)");

                Wait(3000);

                //this.BASE.SYSTEM__O_B_SYSTEM_TIME_SYNC = false;
                this.BASE.DATE_TIME_SET_REQ__O_B_DATE_TIME_SET_REQ = false;

                _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> (Host System Time Sync Bit Off)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion

        #region EOLVISION
        private void __EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)}] {(value ? "Control" : "Maintenance")} Mode");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT__I_B_ALARM_SET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__I_B_ALARM_SET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF)}] : {this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF}");
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
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT__I_W_ALARM_SET_ID.ToString("D6");
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.SET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Set Req No Input Data!!");

                    this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);

                    this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF)}] : true - {this.BASE.ALARM_RPT__I_W_ALARM_SET_ID}");
                    this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF = true;
                }

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }

        private void __ALARM_RPT__I_B_ALARM_RESET_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                bool bLogging = this.BASE.TESTMODE__V_IS_ALMLOG_USE;

                if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__I_B_ALARM_RESET_REQ)}] : {value}");

                if (!value)
                {
                    this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF = value;

                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF)}] : {this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF}");
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
                //inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STATUS__I_W_EQP_STATUS).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EIOSTAT = ((eEqpStatus)this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_STAT).ToString();
                inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_CODE = this.BASE.ALARM_RPT__I_W_ALARM_RESET_ID.ToString("D6");

                //RESET시 ALARMID가 0인 경우 EQPT_ALARM_EVENT_TYPE은 값을 Mapping하지 않게 하여 NULL로 인식할 수 있게 하자.
                if (this.BASE.ALARM_RPT__I_W_ALARM_RESET_ID != 0)
                    inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_ALARM_EVENT_TYPE = ALMTYPE.RESET;

                //입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Alarm Reset Req No Input Data!!");

                    this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                Wait(200); // HDH 2023.09.07 : 설비 상태 보고 와 동시간에 발생하는 것을 방지하기 위해서 200msec 지연 추가

                //BizRule Call
                iRst = BizCall(strBizName, inData, null, out bizEx, string.Empty, bLogging);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0); //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF = true;

                    return;
                }

                //$ 2023.04.20 : TimeOut Test를 쉽게 하기 위해 조건 추가
                if (!this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF)}] : Timeout Test");
                }
                else
                {
                    if (bLogging) _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF)}] : true - {this.BASE.ALARM_RPT__I_W_ALARM_RESET_ID}");
                    this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF = true;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }


        //JH 2024.03.19 : 설비 Smoke Detect 대응 신규 Event Method 추가
        private void __SMOKE_RPT__I_B_SMOKE_DETECT_REQ_OnBooleanChanged(CVariable sender, bool value)
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

                //입력 데이터가 없으면.
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP Smoke Detect Req No Input Data!!");

                    this.BASE.SMOKE_RPT__O_B_EQP_SMOKE_STATUS_CONF = true; //$ 2025.07.02 : Timeout 발생을 막기 위해 비정상 Case의 경우 Bit Trigger후 return

                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, null, out bizEx);
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

        private void __G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G4_6_CELL_INFO_REQ__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF} [{O_W_REP_ACK}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF}");

                    return;
                }
                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //$ 2023.09.01 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
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


                string cellID = IsHR ? this.BASE.G4_6_CELL_INFO_REQ__I_W_CELL_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_CELL_ID);

                string strModelID = IsHR ? this.BASE.G4_6_CELL_INFO_REQ__I_W_MODEL_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_MODEL_ID);
                string strSubModelID = strModelID.Length > 3 ? strModelID.Substring(0, 3) : strModelID; //$ 2021.10.26 : ModelID가 String.Empty여도 괜찮은가?
                string lineID = IsHR ? this.BASE.G4_6_CELL_INFO_REQ__I_W_LINE_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_LINE_ID);

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_CELL_ID))} : {cellID}, {GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_MODEL_ID))} : {strSubModelID}, {GetDesc(nameof(this.BASE.G4_6_CELL_INFO_REQ__I_W_LINE_ID))} : {lineID}");

                Int32 iRst = 0;
                String strBizName = "BR_FORM_CHK_PACKING_CELL";
                Exception bizEx = null;
                string sLog = string.Empty;

                CBR_FORM_CHK_PACKING_CELL_IN inData = CBR_FORM_CHK_PACKING_CELL_IN.GetNew(this);
                CBR_FORM_CHK_PACKING_CELL_OUT outData = CBR_FORM_CHK_PACKING_CELL_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                //$ 2024.11.21 : 사용하지 않는 INDATA 삭제 (IFMODE, FORM_LINEID)
                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].USERID = USERID.EIF;
                inData.IN_EQP[0].EQPTID = this.EQPID;

                inData.IN_EQP[0].MODELID = strSubModelID;

                inData.IN_SUBLOT_LENGTH = 1;
                inData.IN_SUBLOT[0].SUBLOTID = cellID;

                // 입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"Input Cell BCR Read Req No Input Data!! {this.EQPID} {cellID}");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                // BizRule Call -> Current Inspector Tray#1 Arrived
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                // OutData는 없으므로 Biz Error 이외에는 모두 Ack
                HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);

                sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF}-{this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK} - [CELL ID] : {cellID}";
                _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
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

        private void __CELL_INFO__I_B_MEAS_DATA_REQ_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                //_EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.CELL_INFO__I_B_MEAS_DATA_REQ)}]");

                if (!value)
                {
                    //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM = value;
                    //_EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM)}] : {this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM}");
                    return;
                }

                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

                        //HDH 2023.09.12 : Operation Mode가 Off된 상태에서 Request 시 발생하는 Timeout을 막기 위해 99 Nak Reply 추가(해당 Nak에 대한 정상 처리, Trouble 처리는 설비에서 판단)
                        //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM_ACK = (ushort)eConfirm.MNLMODE;
                        //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM = true;
                        return;
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        //$ 2023.03.24 : NG Test Method 추가
                        //bool bOccur = RequestNGReply(System.Reflection.MethodBase.GetCurrentMethod().Name, this.EQPID, nameof(this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM), string.Empty);
                        //if (bOccur) return;
                        return;
                    }
                }

                int iRst = 0;
                string strBizName = "BR_QCA_REG_EQPT_DATA_CLCT";
                Exception bizEx = null;

                CBR_QCA_REG_EQPT_DATA_CLCT_IN inData = CBR_QCA_REG_EQPT_DATA_CLCT_IN.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = this.EQPID;

                string strCellID = IsHR ? this.BASE.G4_6_CELL_INFO_REQ__I_W_CELL_ID.Trim() : _EIFServer.GetSimValue(() => this.BASE.G4_6_CELL_INFO_REQ__I_W_CELL_ID);
                inData.IN_EQP[0].SUBLOTID = strCellID;
                inData.IN_EQP[0].INPUT_SEQ_NO = 1;
                inData.IN_EQP[0].EVENT_NAME = sender.Name;

                #region PLC Data 동적으로 추출                
                this.BizApdItemNameLists.Clear();   //이전 값이 보고 되는 것을 막기 위해 각 Generic들은 모두 초기화 후 다시 데이터를 추출 함
                this.BizApdItemValuesLists.Clear();
                this.BizApdJudgeLists.Clear();

                GetPLCAPDData(strCellID); //PLC Data를 추출하여 APD Data에 Mapping               
                #endregion

                inData.IN_DATA_LENGTH = this.BizApdItemNameLists.Count;
                for (int i = 0; i < this.BizApdItemNameLists.Count; i++)
                {
                    inData.IN_DATA[i].CLCTITEM = this.BizApdItemNameLists[i];

                    for (int j = 0; j < this.BizApdItemValuesLists[this.BizApdItemNameLists[i]].Count; j++)
                    {
                        inData.Variable.Structure[1].StructureList[i].Variables[j + 6].Value = this.BizApdItemValuesLists[BizApdItemNameLists[i]][j]; //Biz Indata에 
                    }
                }

                // 입력 데이터가 없으면.
                if (inData.IN_EQP.Count == 0 && inData.IN_DATA.Count == 0)
                {
                    _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"Cell Request No Input Data!! {this.EQPID}");
                    //TODO : NAK 필요 - Word 영역 필요 & 참조가 0이라 필요한지는 확인 필요
                    return;
                }

                //BizRule Call : Vision 측정 정보
                iRst = BizCall(strBizName, inData, null, out bizEx);
                if (iRst != 0)
                {
                    this.BASE.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM = true;
                    //TODO : NAK 필요 - Word 영역 필요 & 참조가 0이라 필요한지는 확인 필요
                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);
                    return;
                }


                string strJudgBizName = "BR_PRD_REG_PACKING_VISION_JUDG";

                CBR_PRD_REG_PACKING_VISION_JUDG_IN inData_Judg = CBR_PRD_REG_PACKING_VISION_JUDG_IN.GetNew(this);

                #region Equipment : 설비 기본 정보
                inData_Judg.IN_EQPT_LENGTH = 1;
                inData_Judg.IN_EQPT[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData_Judg.IN_EQPT[0].IFMODE = IFMODE.ONLINE;
                inData_Judg.IN_EQPT[0].USERID = USERID.EIF;

                inData_Judg.IN_EQPT[0].EQPTID = this.EQPID;
                #endregion

                #region Sublot : CellID 정보
                inData_Judg.IN_SUBLOT_LENGTH = 1;
                inData_Judg.IN_SUBLOT[0].SUBLOTID = strCellID;
                #endregion

                #region Clct Item : APD 중 Vision Judg 관련
                inData_Judg.IN_CLCT_LENGTH = this.BizApdJudgeLists.Count;

                int idx = 0;
                foreach (KeyValuePair<string, string> Cell in this.BizApdJudgeLists)
                {
                    inData_Judg.IN_CLCT[idx].CLCTITEM = Cell.Key;       //APD Item Name
                    inData_Judg.IN_CLCT[idx].CLCTVALUE = Cell.Value;    //PLC에서 Read한 APD Item Value

                    idx++;
                }
                #endregion

                //BizRule Call -> Judg 정보
                iRst = BizCall(strJudgBizName, inData_Judg, null, out bizEx);
                if (iRst != 0)
                {
                    this.BASE.RegBizRuleException(SIMULATION_MODE, this.EQPID, strJudgBizName, string.Empty, inData_Judg, bizEx);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strJudgBizName} - Biz Exception = {iRst}");

                    //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM = true;
                    //TODO : NAK 필요 - Word 영역 필요 & 참조가 0이라 필요한지는 확인 필요
                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);
                    return;
                }

                //Actual Data 수집 성공
                //this.CELL_INFO__O_B_MEAS_DATA_REQ_CONFIRM = true;

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EOL Vision Inspector Meas Cell_ID_Req Success : Confirm - [CELL ID] -> {(strCellID)}");

            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                //TODO : NAK 필요 - Word 영역 필요 & 참조가 0이라 필요한지는 확인 필요
            }
        }
        #endregion
        #endregion

        #region Word Event Method
        #region EOLVision
        protected virtual void __EQP_STAT_CHG_RPT__I_W_EQP_STAT_OnShortChanged(CVariable sender, ushort value)
        {
            int iRst = -1;
            String strBizName = "BR_SET_EQP_STATUS";
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

                _EIFServer.SetVarStatusLog(this.EQPID, sender, "(EQP State Change)", value);
                _EIFServer.SetVarStatusLog(this.EQPID, sender, "(EQP State Change Start)");

                eEqpStatus state = (eEqpStatus)value;

                if (state == eEqpStatus.T)
                {
                    //$ 2020.10.12 : AlarmCode가 기존 String 3Word에서 Int로 변경되어 ToString에 기본 Format을 입력해줌(5자리 00000)
                    //$ 2020.11.10 : 상태가 4로 변경 된 경우에만 Trouble_CD를 입력
                    //inData.IN_EQP[0].ALARM_ID = this.BASE.EQP_STATUS__I_W_EQP_ALARM_CODE.ToString("D6"); //$ 2022.07.21 : Trouble Code 6자리 표준화 D5 -> D6 변경
                    inData.IN_EQP[0].ALARM_ID = this.BASE.EQP_STAT_CHG_RPT__I_W_ALARM_ID.ToString("D6");

                    HostBizAlarm(string.Empty, this.EQPID, sender, null, 0, false);
                }

                //$ 2020.10.12 : 일단 UserStop(8)인 경우보다 큰 경우에는 8로 고정하고 SubStatus를 입력하게 했음, Wait(2)인 경우에도 처리 필요한지 확인 필요
                //$ 2022.10.27 : Wait 상태에서도 SubState가 보고가 필요하여 조건 수정
                if (state >= eEqpStatus.U || state == eEqpStatus.W)
                {
                    inData.IN_EQP[0].EIOSTAT = (state == eEqpStatus.W) ? state.ToString() : eEqpStatus.U.ToString(); //$ 2023.01.18 : 오타 수정

                    if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                    else
                        inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT.ToString();

                    //this.PreSubState = this.BASE.EQP_STATUS__I_W_EQP_SUBSTATUS;   //$ 2023.05.18 : EQPStatus가 8일 때 SubState 저장하여 SubState Event 처리를 Skip할지 또 할지 결정
                    this.PreSubState = this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT;
                    Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌
                }
                else
                {
                    inData.IN_EQP[0].EIOSTAT = state.ToString();
                    inData.IN_EQP[0].LOSS_CODE = string.Empty;
                    this.PreSubState = 0;   //$ 2023.05.18 : EQPStatus가 8일 아닐 때 초기화
                }

                iRst = BizCall(strBizName, inData, outData, out bizEx);
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
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
            }
        }

        //$ 2023.05.18 : EQPState가 8인 상태에서 SubState만 바꿀 경우 EIF에서 처리가 불가능한 상황이 있어 따로 SubState도 이벤트 처리함(엔솔 요청)
        protected virtual void __EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT_OnShortChanged(CVariable sender, ushort value)
        {
            string strEQPID = this.EQPID;
            int iRst = -1;
            Exception bizEx;
            string strBizName = "BR_SET_EQP_STATUS";
            try
            {
                if (value < 8) return;                                            //$ 2023.06.05 : SubState 8이하로 바뀌는 것은 처리 안함.
                if (this.PreSubState == value) return;   // SubState가 EQPState에서 변경되지 않는 상태(8 이외 상태)이거나 EQPState 8에서 바뀐 값과 같다면 SubState 처리 필요 없음(중복 보고 막음) // HDH 2023.07.14 : PreSubState == 0 조건 삭제
                //if (this.BASE.EQP_STATUS__I_W_EQP_STATUS != 8) return;                 // EQPStatus가 8이 아닌 경우 SubState가 없으므로 처리할 필요 없음 
                if (this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_STAT != 8) return;                 // EQPStatus가 8이 아닌 경우 SubState가 없으므로 처리할 필요 없음 

                this.PreSubState = value;

                Wait(100); //$ 2025.07.07 : Main Userstop과 SubState가 동시에 바뀌는 경우를 처리하기 위해 SubState에 Delay를 줌

                CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);

                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = strEQPID;

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change)", value);
                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change Start)");

                inData.IN_EQP[0].EIOSTAT = eEqpStatus.U.ToString();

                if (this.EQP_INFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                    inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT < 100) ? "000000" : this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT.ToString("D6"); //$ 2023.07.07 : Substate 100 이하는 0으로 치환 //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                else
                    inData.IN_EQP[0].LOSS_CODE = (this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT < 100) ? "0" : this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_SUBSTAT.ToString();

                iRst = BizCall(strBizName, inData, outData, out bizEx);
                if (iRst == 0)
                {
                    _EIFServer.SetBizRuleLog(strEQPID, strBizName, inData.Variable, outData.Variable);

                    if (outData.OUTDATA_LENGTH > 0 && outData.OUTDATA[0].RETVAL == 0)
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizName} [RETVAL = > OK]");
                    }
                    else
                    {
                        _EIFServer.SetVarStatusLog(strEQPID, sender, $"{strBizName} [RETVAL = > NG]");
                    }
                }
                else
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);
                }

                _EIFServer.SetVarStatusLog(strEQPID, sender, "(EQP Sub State Change End)");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(strEQPID, sender, ex);
            }
        }

        protected virtual void __EQP_OP_MODE_CHG_RPT__I_W_HMI_LANG_TYPE_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"GP Language : {(value)}");

                _EIFServer.SetLanguageID(this.EQPID, value);  //$ 2023.12.14 : PLC 언어 설정 변경 시 언어 값 저장
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
            if (value)
            {
                Task.Factory.StartNew(() =>
                {
                    if (CVariableAction.TimeOut(this.Owner[sender.NameCategorized], false, SCANINTERVAL, SECINTERVAL))
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
        //JH 2025.06.09 EIF 재기동 시 RMS connect ON 하는 메소드 이나 현재 Vision에서느 RMS를 사용하지 않음
        public virtual void SetRemoteCommand(ushort uCode)
        {
            switch (uCode)
            {
                case 1:  //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE = uCode;
                    this.BASE.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }
        #endregion 
        #region Vision
        //$ 2020.11.10 : UI에서 Remote Command를 받아야 하는 구조인데.. UI에서 줄수 있을지 없을지 몰라 일단 가상 변수로 처리 함
        protected virtual void __REMOTE_COMM_SND__V_REMOTE_CMD_OnShortChanged(CVariable sender, ushort value)
        {
            switch (value)
            {
                case 1: //RMS Control State Change to Online Remote
                case 12: //IT Bypass Mode Release
                case 21: //Processing Pause
                    this.BASE.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE = value;
                    this.BASE.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = true;
                    break;

                default:
                    break;
            }
        }

        //$ 2020.11.10 : EIF에 요청한 Remote Command에 대한 Confirm 처리
        protected virtual void __REMOTE_COMM_SND__I_B_REMOTE_COMMAND_CONF_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{sender.Description} : ", value);

                if (value)
                {
                    string sLog = $"REMOTE__I_B_REMOTE_COMMAND_CONFIRM : {value}, REMOTE_COMM_SND__I_W_REMOTE_COMMAND_CONF_ACK : {this.BASE.REMOTE_COMM_SND__I_W_REMOTE_COMMAND_CONF_ACK}";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                    this.BASE.REMOTE_COMM_SND__O_W_REMOTE_COMMAND_CODE = 0;
                    this.BASE.REMOTE_COMM_SND__O_B_REMOTE_COMMAND_SEND = false;
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

                int eqpCnt = this.TactEQPIDs.Length;
                for (int i = 0; i < this.TactEQPIDs.Length; i++)
                {
                    if (string.IsNullOrEmpty(this.TactEQPIDs[i])) continue;

                    string unitID = this.TactEQPIDs[i];

                    //설비 상태가 단인 경우 EQP_STAT_CHG_RPT 처럼 _01이 안붙기에, 대응을 위한 코드 수정
                    ushort tactTime = eqpCnt > 1 ? this.BASE.Variables[$"EQP_STAT_CHG_RPT_{i + 1:D2}:I_W_EQP_TACT_TIME"].AsShort : this.BASE.Variables[$"EQP_STAT_CHG_RPT:I_W_EQP_TACT_TIME"].AsShort;

                    //$ 2022.09.30 : Tact 관련 정보가 0이라도 0으로 보고 할 수 있도록 하기 조건 주석 처리
                    //if (tactTime == 0)
                    //{
                    //    this.SetLog(eqpIDs[i], "TACT_TIME = 0 Biz Report Pass");
                    //    continue;
                    //}

                    //설비 상태가 단인 경우 EQP_STAT_CHG_RPT 처럼 _01이 안붙기에, 대응을 위한 코드 수정
                    eEqpStatus state = eqpCnt > 1 ? (eEqpStatus)this.BASE.Variables[$"EQP_STAT_CHG_RPT_{i + 1:D2}:I_W_EQP_STAT"].AsShort : (eEqpStatus)this.BASE.Variables[$"EQP_STAT_CHG_RPT:I_W_EQP_STAT"].AsShort;

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
                    int iRst = BizCall("BR_EQP_REG_EQPT_OPER_INFO", inData, null, out bizEx);
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
        protected virtual void HostConfirmReset()
        {

            if (this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF) //$ 2021.10.20 : 누락된 Bit에 대한 초기화 추가
            {
                this.BASE.G4_6_CELL_INFO_REQ__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.DEFAULT;
                this.BASE.G4_6_CELL_INFO_REQ__O_B_TRIGGER_REPORT_CONF = false;
            }

            //$ 2023.02.01 : 누락되어 추가
            this.BASE.HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND = false;

            //$ 2023.02.01 : 신규 설비 Alarm 대응 추가
            this.BASE.ALARM_RPT__O_B_ALARM_SET_CONF = false;
            this.BASE.ALARM_RPT__O_B_ALARM_RESET_CONF = false;
        }

        //$ 2023.05.18 : Biz 실행 Log Flag 인자 추가, EIF Log 및 Biz Log는 해당 인자에 맞게 처리
        public int BizCall(string bizName, CVariable inVariable, CVariable outVariable, out Exception bizEx, bool bLogging = true)
        {
            bizEx = null;

            DateTime preTime = DateTime.Now;
            if (bLogging) _EIFServer.SetLog("BIZ", $"{bizName} - Start");

            int iRst = (SIMULATION_MODE) ? 0 : _EIFServer.FAService.Request(bizName, inVariable, outVariable, out bizEx, bLogging);

            if (bLogging) _EIFServer.SetLog("BIZ", $"{bizName} - End {iRst}, [{(DateTime.Now - preTime).TotalSeconds:0.000}s]");

            return iRst;
        }

        //$ 2024.10.16 : SolaceBiz 실행 Log Flag 인자 추가, EIF Log 및 Biz Log는 해당 인자에 맞게 처리
        public int BizCall(string bizName, CStructureVariable inVariable, CStructureVariable outVariable, out Exception bizEx, string txnID = "", bool bLogging = true)
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

            SolaceLog(this.EQPID, txnID, 3, $"{bizName} : {inVariable.Variable.ToString()}");

            SolaceLog(this.EQPID, txnID, 4, $"{bizName}"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

            _EIFServer.EnableLoggingBizRule = bLogging;

            int iRst = (SIMULATION_MODE) ? 0 : _EIFServer.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BizTimeout, out bizEx);

            _EIFServer.EnableLoggingBizRule = true;

            SolaceLog(this.EQPID, txnID, 4, $"{bizName} [{(DateTime.Now - preTime).TotalMilliseconds:0.0}ms] - {iRst}");
            //if (bLogging) _EIFServer.SetLog("BIZ", $"{bizName} - End {iRst}, [{(DateTime.Now - preTime).TotalSeconds:0.000}s]");

            if (iRst == 0 && outVariable != null)
                SolaceLog(this.EQPID, txnID, 5, $"{bizName} : {outVariable.Variable.ToString()}");

            return iRst;
        }

        //$ 2023.03.24 : 원활한 설비 테스트를 위해 NAK Test와 TimeOut Test를 동시에 선택한 경우 Nak한번 Timeout한번 발생 후 정상진행하게 하여 다음 Event 테스트하게 함(Test 개별 선택시에는 기존과 동일)
        protected bool RequestNGReply(string methodName, string eqpID, string boolProperty, string shortProperty)
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

        protected virtual void LoadCLCTItems()
        {
            DataSet ds = new DataSet();
            string strSQL = string.Empty;

            Dictionary<int, CClctItem> _ClctItemLists = new Dictionary<int, CClctItem>();    //$ 2021.10.28 : DB에 저장된 값을 저장할 Dictionary

            //$ 2024.11.21 : Oracle Query 변경건
            if (this.Variables.ContainsKey("BASICINFO:V_EQP_UNIT_ID_01"))
            {
                strSQL = string.Format("SELECT * FROM TB_CLCTITEM_STND WHERE EQP_ID = '{0}' ORDER BY CLCTITEM_NO, CLCTTYPE", this.Variables["BASICINFO:V_EQP_UNIT_ID_01"].AsString);
            }
            else
            {
                strSQL = string.Format("SELECT * FROM TB_CLCTITEM_STND WHERE EQP_ID = '{0}' ORDER BY CLCTITEM_NO, CLCTTYPE", this.EQPID);
            }

            using (CDataManager mgr = new CDataManager())
            {
                mgr.GetDataSet(ds, "TB_CLCTITEM_STND", strSQL);
            }

            for (int i = 0; i < ds.Tables["TB_CLCTITEM_STND"].Rows.Count; i++)
            {
                CClctItem Item = new CClctItem(i);

                Item.EQPID = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["EQP_ID"].ToString().Trim();
                Item.CLCTTYPE = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTTYPE"].ToString().Trim();
                Item.CLCTITEMNO = (int)ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM_NO"];
                Item.CLCTITEM = ds.Tables["TB_CLCTITEM_STND"].Rows[i]["CLCTITEM"].ToString().Trim();
                Item.FPOINT = (int)ds.Tables["TB_CLCTITEM_STND"].Rows[i]["FPOINT"];

                _ClctItemLists.Add(i, Item);
            }

            this.ApdItemNameLists = _ClctItemLists.Where(x => x.Value.CLCTTYPE == "APD_DATA").ToDictionary(x => x.Value.CLCTITEMNO, x => x.Value.CLCTITEM);
            this.ApdItemFPointLists = _ClctItemLists.Where(x => x.Value.CLCTTYPE == "APD_DATA").ToDictionary(x => x.Value.CLCTITEMNO, x => x.Value.FPOINT);
        }

        //$ 2021.10.28 :  APD Item Name, Values(3개 정도), Vision 판정값 등을 PLC에서 추출
        protected virtual void GetPLCAPDData(string strCellID)
        {
            int nTempVal = 0;
            ushort uTempVal = 0;
            double dCalVal = 0;

            for (int i = 1; i <= this.ADD_INFO__V_CELL_CLCT_DATA_CNT; i++)
            {
                if (!string.IsNullOrWhiteSpace(this.ApdItemNameLists[i]))
                {
                    List<string> tmpAddValues = new List<string>();  //PLC 데이터 및 CellID 정보등을 임시 저장 할 List

                    string strApdItemName = this.ApdItemNameLists[i];
                    int nApdFPoint = this.ApdItemFPointLists[i];

                    string strVarName = $"UV_APDDATA:APD_CELL_{1:D2}_DATA_{(i):D3}";
                    int nCellPosition = i;

                    switch (Variables[strVarName].DataType)
                    {
                        case enumDataType.String:
                            {
                                string sValue = IsHR ? Variables[strVarName].AsString.Trim() : _EIFServer.GetSimValue<string>(strVarName);

                                tmpAddValues.Add(sValue);
                                tmpAddValues.Add(nCellPosition.ToString());
                                tmpAddValues.Add(strCellID);

                                // 재작업시 판정값 확인위해 추가 보고 - APD 항목변경시 반드시 확인 필요
                                if (i > 65) this.BizApdJudgeLists.Add(strApdItemName, Variables[strVarName].AsString.Trim()); //$ 2021.10.28 : GMJV용 Judg는 사양서 상 66번 부터 시작이므로 65 이상으로 조건 건 듯..
                            }
                            break;

                        case enumDataType.Integer:
                            {
                                nTempVal = IsHR ? Variables[strVarName].AsInteger : _EIFServer.GetSimValue<int>(strVarName);
                                dCalVal = nTempVal / Math.Pow(10, nApdFPoint);

                                tmpAddValues.Add(string.IsNullOrWhiteSpace(dCalVal.ToString()) ? string.Empty : dCalVal.ToString());
                                tmpAddValues.Add(nCellPosition.ToString());
                                tmpAddValues.Add(strCellID);
                            }
                            break;

                        case enumDataType.Short:
                            {
                                uTempVal = IsHR ? Variables[strVarName].AsShort : _EIFServer.GetSimValue<ushort>(strVarName);
                                dCalVal = nTempVal / Math.Pow(10, nApdFPoint);

                                tmpAddValues.Add(string.IsNullOrWhiteSpace(dCalVal.ToString()) ? string.Empty : dCalVal.ToString());
                                tmpAddValues.Add(nCellPosition.ToString());
                                tmpAddValues.Add(strCellID);
                            }
                            break;

                        case enumDataType.Boolean:
                            {
                                bool bFlag = IsHR ? Variables[strVarName].AsBoolean : _EIFServer.GetSimValue<bool>(strVarName);

                                tmpAddValues.Add(bFlag ? "OK" : "NG");
                                tmpAddValues.Add(nCellPosition.ToString());
                                tmpAddValues.Add(strCellID);
                            }
                            break;

                        case enumDataType.ShortList:
                            List<int> ilCellPrintTime = new List<int>();

                            if (IsHR)
                            {
                                foreach (ushort us in this.Variables[strVarName].AsShortList)
                                    ilCellPrintTime.Add(us);
                            }
                            else
                                ilCellPrintTime = _EIFServer.GetSimLists<int>(strVarName);

                            DateTime dt = new DateTime(ilCellPrintTime[0], ilCellPrintTime[1], ilCellPrintTime[2], ilCellPrintTime[3], ilCellPrintTime[4], ilCellPrintTime[5]);

                            tmpAddValues.Add(dt.ToString());
                            tmpAddValues.Add(nCellPosition.ToString());
                            tmpAddValues.Add(strCellID);
                            break;
                    }


                    this.BizApdItemNameLists.Add(strApdItemName);
                    this.BizApdItemValuesLists.Add(strApdItemName, tmpAddValues);
                }
            }
        }
        #endregion

        #region EOLVision
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

                HostAlarm(sender, sTroubleCD, sMessage, uType, bFlag);
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
            }
        }

        private void HostAlarm(CVariable sender, string sTroubleCD, string sMessage, ushort uType, bool bFlag = true)
        {
            int iTroubleCD = 0;

            //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
            if (bFlag)
            {
                //$ 2023.01.03 : Biz Trouble Code가 숫자 변환이 안되는 경우 처리, 현재 6만번때 Host Trouble이 없어 6만을 Default값으로 사용
                if (!int.TryParse(sTroubleCD, out iTroubleCD))
                    iTroubleCD = EIFALMCD.DEFAULT;

                List<string> hostMsg = new string[] { $"{uType}_{sMessage}", "" }.ToList();

                this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG = hostMsg; //$ 2023.02.23 : 사양에 맞게 EQP_STATUS__O_W_HOST_ALARM_MSG 배열로 변경
                //JH 2025.06.04 명시적으로 표현하기 위해 추가해 봅니다..
                this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_SEND_SYSTEM = 0;
                this.BASE.HOST_ALARM_MSG_SEND__O_W_EQP_PROC_STOP_TYPE = 0;

                this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID = iTroubleCD;
                this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_DISP_TYPE = uType;
            }

            this.BASE.HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND = bFlag;

            string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
            if (bFlag)
                sLog = $"{sLog} - Type : {uType}, Alarm Code : {iTroubleCD}";

            _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
        }

        private void HostAlarm(int iTroubleCD, string sMessage, ushort uType, bool bFlag, ushort stopType, ushort action)
        {
            lock (_lockHostAlm)
            {
                //$ 2023.01.12 : 설비에서 Host Trouble 체크를 못하는 경우가 있어 Word 영역은 Clear하지 않게 변경함
                if (bFlag)
                {
                    List<string> hostMsg = new string[] { sMessage, "" }.ToList();

                    this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_MSG = hostMsg;
                    this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_ID = iTroubleCD;
                    this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_DISP_TYPE = uType;

                    this.BASE.HOST_ALARM_MSG_SEND__O_W_HOST_ALARM_SEND_SYSTEM = stopType;
                    this.BASE.HOST_ALARM_MSG_SEND__O_W_EQP_PROC_STOP_TYPE = action;
                }

                this.BASE.HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND = bFlag;

                string sLog = $"HOST Trouble - {uType} [{nameof(this.BASE.HOST_ALARM_MSG_SEND__O_B_HOST_ALARM_MSG_SEND)}] : {bFlag}";
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

                this.MONITOR__V_MONITOR_EQPSTATUS = GetStringEqpStat(this.BASE.EQP_STAT_CHG_RPT__I_W_EQP_STAT);
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
        #endregion
    }
}