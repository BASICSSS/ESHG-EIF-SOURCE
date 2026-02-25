using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Core;
using LGCNS.ezControl.Driver.Serial;
using LGCNS.ezControl.EIF.Solace;
using LGCNS.ezControl.Solace;

using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.CDC
{
    public enum eOperGroup { CHARGE = 11, DISCHARGE, OCV, IMP, POWERGRD = 17, POWERCHG = 19 };
    public partial class CGCDC : CImplement, IEIF_Biz
    {
        #region Simulation Mode 설정 관련
        public const Boolean SIMULATION_MODE = false; //$ 2021.07.06 : 사전 검수 모드는 빌드를 통해서만 바꿀수 있게 하자

        private ushort ContiStep { get; set; }
        private eOperGroup[] m_ArrContWorks = { eOperGroup.OCV, eOperGroup.CHARGE, eOperGroup.DISCHARGE };
        private eOperGroup[] m_ArrContPowWorks = { eOperGroup.POWERCHG, eOperGroup.POWERGRD };
        #endregion

        //$ 2019.05.16 : Header의 Protocol별 Size를 저장하여 공통 관리함.
        protected const int HEADERSIZE = 33;
        protected const int DIRSIZE = 1;
        protected const int OBJECTIDSIZE = 20;
        protected const int TYPESIZE = 1;
        protected const int REPLYSIZE = 1;
        protected const int MSGIDSIZE = 6;
        protected const int SEQNOSIZE = 4;

        #region Box Constant
        protected const string HOSTDIRECT = "H";

        protected const string NO_REPLY = "N";   //$ 2020.08.14 : 0 -> N, 1->R로
        protected const string EX_REPLY = "R";

        protected const string REPLY = "R"; //$ 2020.07.28 : const로 저장하여 사용
        protected const string SEND = "S"; //$ 2020.08.27 : const로 저장하여 사용

        protected const string OK_RET = "0";
        protected const string NG_RET = "1";
        protected const string EXCEPTION_ERR_RET = "9";

        protected const string UNKNOWN_CMD_ERR = "1";
        //protected const string SIZE_ERR = "2";   //$ JSON 구조로 변경되어 Size Error 없음
        protected const string CHECK_SUM_ERR = "3";

        protected const string DIRECTION_ERROR_RET = "2";

        //TC_EQP_STATUS Table의 EQP_CTR_STATUS_CD 의 값
        protected const string OPER_PAUSE = "P";          //작업 일시중지 : P -> T
        protected const string OPER_RESUME = "S";         //연속 시작 : S -> W
        protected const string OPER_RESTART = "Q";        //공정 재시작 : Q -> U
        protected const string OPER_RESET = "I";          //설비 초기화 : I -> J
        protected const string OPER_TRAY_UNLOAD = "F";    //Tray Unload : F -> N
        protected const string OPER_TRAY_RTOR = "L";      //Tray Rack to Rack : L -> K
        protected const string OPER_END = "E";            //현재 공정 종료 : E -> V

        protected const string BEFORE_DAGAS = "1";
        protected const string AFTER_DEGAS = "2";

        protected const int MAX_SEQ = 9999; //$ 2020.06.01 : 99999 -> 9999 4자리 변경

        //$ 2018.12.02 : 910에서 각 장비별로 몇 회 후 온도 데이터를 저장할지 저장할 Dictionary
        private Dictionary<string, int> Temp_Report_Cnt { get; set; }

        //$ 2021.07.15 : GMJV EqpID 체계는 EMS ID 체계를 따르며 충방전 Unit(14자리), Jig Form Ass(19자리), 대용량/전용OCV Main(8자리) 체계로 운영
        //               단 Header의 경우 가변이 되면 안되므로 20자리를 기본으로 사용 빈 자리수는 Space로 채워 20자리 유지 시킴
        //               Body의 경우 Json 구조라 가변을 허락하므로 EqpID고유의 자리수를 사용함
        private string m_HostObjectID = string.Empty;
        private string HostObjectID // Header에 들어갈 EqpID 계산용 Property
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_HostObjectID))
                    m_HostObjectID = this.HostObjID;

                if (this.m_HostObjectID.Length < 20)
                {
                    if (this.HostObjID.Length < 20)
                    {
                        m_HostObjectID = this.HostObjID.PadRight(20, ' ');
                    }
                }

                return m_HostObjectID;
            }
        }

        //$ 2021.10.19 : UI에서 Port 배출 요청시 설비에서 UR시 MHS로 Port 배출임을 명시적으로 전달하기 위해 추가
        private Dictionary<string, bool> ReqTrayOutFlag { get; set; }

        //$ 2021.12.22 : Recipe 요청 시 Biz Error(정확하게는 TrayID 누락) 발생하는 경우 바로 Host Trouble 발생을 막기 위해 추가
        private Dictionary<string, bool> IsBeforeBizErrOccur { get; set; }

        private Dictionary<string, bool> NakPassList = null; //$ 2023.05.16 : 주요 HandShake Nak Test시 Host Alarm 이력을 저장하여 Message별로 1회씩 Host Trouble 발생 시킴
        private Dictionary<string, bool> TimeOutPassList = null;//JH 2024.03.15 : NOREPLY 대한 이력 저장 

        /// <summary>
        /// Solace Request Queue Name
        /// </summary>
        private string ReqQueue { get { return this.BIZINFO__V_REQQUEUE_NAME; } }

        /// <summary>
        /// Solace Reply Queue Name
        /// </summary>
        private string RepQueue { get { return $"REPLY/{this.BIZINFO__V_REQQUEUE_NAME}"; } }

        private string SendTnxID
        {
            get
            {
                return DateTime.Now.ToString("yyyyMMddHHmmss.fff");
            }
        }

        protected CSolace SmokeSolChannel = new CSolace();  //$ 2025.07.16 : 통합 관제 솔라스 보고용 객체

        #endregion

        protected override CElement Owner => CExecutor.ElementsByElementPath.First().Value;
        private CSolaceEIFSocketServerBizRule _EIFServer = null;

        private CCDC BASE { get { return (Owner as CCDC); } }

        string HostObjID { get { return this.BASE.BASICINFO__V_HOST_OBJECT_ID; } }

        #region Varialbe Define
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();


            //$ 2018.12.02 : Fatory Lync Modeler에 신규 Virtual 변수 추가
            __INTERNAL_VARIABLE_INTEGER("V_TEMP_COUNT", "EQPINFO", enumAccessType.Virtual, 0, 0, false, true, 1, "", "EEER CEID = 651 Temp Data DB Insert Count");
            __INTERNAL_VARIABLE_INTEGER("V_INTERVAL", "EQPINFO", enumAccessType.Virtual, 0, 0, false, true, 300, "", "EERR Gap Request Interval(msec)");
            __INTERNAL_VARIABLE_INTEGER("V_PORT_TYPE", "EQPINFO", enumAccessType.Virtual, 0, 0, false, true, 0, "", "PortType - 0 : Both Port, 1 : Load Port, 2 : Unload Port, 3 : LD/ULD Port 혼합");
            __INTERNAL_VARIABLE_STRING("V_MACHINE_ID", "EQPINFO", enumAccessType.Virtual, false, true, "", "", "MACHINE_ID - MainEQPID를 제외한 내역만 기재");            //$ 2022.05.11 : MHS를 위해 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_POWER_BOX", "EQPINFO", enumAccessType.Virtual, true, false, false, "", "False - 충방전기, True - 대용량 방전기");           //$ 2022.05.11 : MHS를 위해 추가            

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_SIXLOSSCODE_USE", "EQPINFO", enumAccessType.Virtual, true, false, false, "", "True - Loss Code 6자리 사용, False - 기존 처럼 3자리 사용");    //$ 2023.07.26 : Loss Code 3자리 or 6자리 사용 여부

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_EUM_USE", "EQPINFO", enumAccessType.Virtual, true, false, false, "", "False - EUM 사용 안함, True - EUM 사용");     //$ 2024.12.05 : 전력량 및 유량계 사용 여부 설정

            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "BIZINFO", enumAccessType.Virtual, false, true, "", "", "EIF -> Biz Server Request Queue Name");
            __INTERNAL_VARIABLE_INTEGER("V_BIZCALL_TIMEOUT", "BIZINFO", enumAccessType.Virtual, 0, 0, true, false, 30000, string.Empty, "Biz Call TimeOut(mSec)");

            //$ 2025.07.16 : Smoke Solace 접속 정보 및 요청 Queue를 직접 등록해 줌
            __INTERNAL_VARIABLE_STRING("CONNECTION_INFO", "SMOKE_SOLACE", enumAccessType.Virtual, false, false, "HOST=,USER=,VPNNAME=,PASSWORD=,TOPICS=", "", "");
            __INTERNAL_VARIABLE_STRING("V_REQQUEUE_NAME", "SMOKE_SOLACE", enumAccessType.Virtual, false, true, "", "", "EIF -> Smoke Server Request Queue Name");

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
            #endregion

            this.Temp_Report_Cnt = new Dictionary<string, int>(); //$ 2018.12.02 : 인스턴스 생성
            this.ReqTrayOutFlag = new Dictionary<string, bool>(); //$ 2021.10.19 : 인스턴스 생성
            this.IsBeforeBizErrOccur = new Dictionary<string, bool>(); //$ 2021.12.22 : 인스턴스 생성
        }
        #endregion

        #region 00. Add BizRule Null
        protected override void OnInitializeCompleted()
        {
            base.OnInitializeCompleted();
            _EIFServer = (CSolaceEIFSocketServerBizRule)Owner;

            _EIFServer.HandleEmptyStringByNull = true;

            if (string.IsNullOrWhiteSpace(ReqQueue) == true)
                _EIFServer.SetLog("[STATUS] Solace Request Queue Name is empty!!!.", this.HostObjID, this.HostObjID);

            _EIFServer.SetSolaceInfo(this.ReqQueue, this.RepQueue, this.BIZINFO__V_BIZCALL_TIMEOUT);

            this.MONITOR__V_MONITOR_EQUIPMENT_ID = this.HostObjectID;
            this.MONITOR__V_MONITOR_EQP_NICNAME = _EIFServer.Description;  //$ 2025.10.31 : NICKNAEM 항목에 ControlServer Desctripion을 보여 주기로 함
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.ONLINE.ToString();
            this.MONITOR__V_MONITOR_BIZ_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.MONITOR__V_MONITOR_BASE_HOSTNAME = Environment.MachineName;
            this.MONITOR__V_MONITOR_LOCAL_HOST_IP = _EIFServer.GetHostIP(_EIFServer.Variables["SOLACE:CONNECTION_INFO"].ToString());
            this.MONITOR__V_MONITOR_FACTOVA_VER = Assembly.GetEntryAssembly().GetName().Version.ToString();
            this.MONITOR__V_MONITOR_SCAN_INTERVAL = ""; // TCPIP라 Interval이없음       

        }
        #endregion

        protected override void OnStarted()
        {
            base.OnStarted();
            _EIFServer.SetLog("[STATUS] EIF FactoryLync(L2) Started", this.HostObjID, this.HostObjID);

            //base.SocketServer.EnableLog(false, false); //$ 2021.08.04 : System에 TCP Port별로 Raw Log를 남길지 여부를 설정, Modeler에서도 Config 설정으로 막을 수 있음
            //PORT = 9999, ENABLE_LOG_ASCII = 0, ENABLE_LOG_HEX = 0 //모델러 Port 설정에 추가 하면 됨

            EIFMonitoringData();

            //TimerSearch(); //$ 2025.10.31 : Scheduler Interval 이후 호출 되는 것이 문제가 된다면 OnStarted에서 명시적으로 호출 후 이후 Interval대로 반복 호출 함
        }

        protected override void OnStopped()
        {
            _EIFServer.SetLog("Program Formation Stopped", this.HostObjID, this.HostObjID);

            base.OnStopped();
        }

        protected override void OnUnloaded()
        {
            _EIFServer.SetLog("Program Formation Exit", this.HostObjID, this.HostObjID);

            this.MONITOR__V_MONITOR_SOLACE = enumConnectionState.Disconnected.ToString();
            this.MONITOR__V_MONITOR_HOST_COMMUNICATIONSTATE = CommunicationState.OFFLINE.ToString();

            base.OnUnloaded();
        }

        protected override void OnInstancingCompleted()
        {
            base.OnInstancingCompleted();

            #region Remote Command
            this.BASE.__TESTMODE__V_IS_NAK_TEST.OnBooleanChanged += __TESTMODE_BIT_OnBooleanChanged; //$ 2023.05.16 : Nak Test Off시 Dictionary Clear 시킴
            this.BASE.__TESTMODE__V_IS_TIMEOUT_TEST.OnBooleanChanged += __TESTMODE_BIT_OnBooleanChanged; //$ 2025.07.11 : Timeout Test Off시 Dictionary Clear 시킴
            #endregion

            //$ 2025.10.31 : 기존 100ms 후 빠르게 Scheduler 함수 호출하고 Wait로 Interval 조정하던 것을 정상적인 Process로 처리(프로그램 시작하자마 호출 필요 시 따로 Scheduler 함수 호출)
            __SCHEDULER(TimerSearch, this.BASE.EQPINFO__V_WAITTIME, true); //$ 2025.07.26 : 초기 Scheduler 시작 시간을 짧게 설정하고 해당 Scheduler 함수 안에서 Wait로 시간 조정
            //SchedulerHandlers.First().Value.Run(); //RunAtStartUp = falas일 경우 Thread Method 구동하는 방법

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
            __MONITOR__V_MONITOR_SCAN_INTERVAL.SystemMonitoring = false;            //CImp에서

            __MONITOR__V_MONITOR_BASE_HOSTNAME.SystemMonitoring = true;             //CImp에서
            __MONITOR__V_MONITOR_NOTIFICATION.SystemMonitoring = false;              //??

            __MONITOR__V_MONITOR_LOCAL_HOST_IP.SystemMonitoring = true;
            __MONITOR__V_MONITOR_SOLACE.SystemMonitoring = true;
            __MONITOR__V_MONITOR_FACTOVA_VER.SystemMonitoring = true;

            //$ 2025.07.15 : Smoke Solace 연결 정보를 통해 Channel만 Open 해둠
            CConnectionString conn = new CConnectionString(this["SMOKE_SOLACE:CONNECTION_INFO"].AsString);
            this.SmokeSolChannel.Initialize(this, this.Name, conn["HOST", ""], conn["VPNNAME", ""], conn["USER", ""], conn["PASSWORD", ""], conn["TOPICS", ""], conn["QUEUES", ""], conn["ASYNC_EVENT", 1] == 1);
        }

        public void OnConnected(CSocketClient client)
        {
            try
            {
                _EIFServer.SetLog("Connect : " + client.Id, this.HostObjID, client.Id);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, client.Id);
            }
            finally
            {
                //Client 접속시 해당 EQP ID를 알수 없음- Cmd 925 전송 후 EQP ID Mapping
                if (this.BASE.DicClientEqpID.ContainsKey(client.Id))
                {
                    this.BASE.DicClientEqpID[client.Id] = null;
                }
                else
                {
                    this.BASE.DicClientEqpID.Add(client.Id, null);
                }
            }
        }

        public void OnDisconnect(CSocketClient client)
        {
            String strBizName = "BR_SET_EIF_COMM_STATUS";
            Exception bizEx;

            try
            {
                // Client Connect 후 Msg 없이 DisConnect시 EQP_ID가 없음
                //EQP_ID NULL 일경우 BizActor 호출 Pass
                if (string.IsNullOrEmpty(this.BASE.DicClientEqpID[client.Id]))
                {
                    _EIFServer.SetLog($"DisConnect : {client.Id} return", this.HostObjID, this.BASE.DicClientEqpID[client.Id]);
                    return;
                }

                //$ 2022.12.19 : 연결이 종료되고 바로 다시 연결이 될 경우 이전 Port에 대한 Disconnect 처리로 인해 실제 통신은 되지만 DB는 CommState가 Off인 경우가 있어 수정 //$ 2025.05.22 : 누락 내역 추가
                Wait(50);
                if (this.BASE.DicClientEqpID.Where(r => r.Value == this.BASE.DicClientEqpID[client.Id]).Count() > 1)
                {
                    string aliveID = this.BASE.DicClientEqpID.Where(r => r.Key != client.Id && r.Value == this.BASE.DicClientEqpID[client.Id]).First().Key.ToString();
                    _EIFServer.SetLog($"DisConnect : {client.Id}, But New Connection{aliveID} Exist - Return", this.HostObjID, this.BASE.DicClientEqpID[client.Id]);
                    return;
                }
                else
                {
                    _EIFServer.SetLog("DisConnect : " + client.Id, this.HostObjID, this.BASE.DicClientEqpID[client.Id]);
                }

                CBR_SET_EIF_COMM_STATUS_IN inData = CBR_SET_EIF_COMM_STATUS_IN.GetNew(this);
                CBR_SET_EIF_COMM_STATUS_OUT outData = CBR_SET_EIF_COMM_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = this.BASE.DicClientEqpID[client.Id];
                inData.IN_EQP[0].EIOCOMSTAT = eComStatus.OFF.ToString();

                int iRst = BizCall(strBizName, this.BASE.DicClientEqpID[client.Id], inData, outData, out bizEx);
                if (iRst != 0)
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.BASE.DicClientEqpID[client.Id], strBizName, string.Empty, inData, bizEx);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, client.Id + ":" + this.BASE.DicClientEqpID[client.Id]);
            }
            finally
            {
                this.BASE.DicClientEqpID.Remove(client.Id);
            }
        }

        public void OnClientReceivedBytes(CSocketClient client, byte[] packet)
        {
            StringBuilder buff = null;
            if (!client.CustomData.ContainsKey("BUFFER"))
            {
                buff = new StringBuilder();
                client.CustomData["BUFFER"] = buff;
            }
            else
                buff = (StringBuilder)client.CustomData["BUFFER"];

            for (int i = 0; i < packet.Length; i++)
            {
                switch ((byte)packet[i])
                {
                    case 0x02:
                        buff.Clear();
                        break;
                    case 0x03:
                        RevMsg(client, buff.ToString());
                        break;
                    default:
                        buff.Append((char)packet[i]);
                        break;
                }
            }
        }

        private void RevMsg(CSocketClient client, string strPacket)
        {
            CPacket ClientPacket = new CPacket(client);
            try
            {
                string sMsg = strPacket.Substring(0, strPacket.Length - 2);
                string sChecksum = strPacket.Substring(strPacket.Length - 2);

                //Message Parsing
                ParseMsg(ClientPacket, sMsg);

                //Check Sum
                if (ClientPacket.GetCheckSum(sMsg) != sChecksum)
                {
                    ReceiveMessageLogWrite(ClientPacket, "", true); //$ 2023.01.05 : 테스트 무관하므로 true
                    HostUnknownMessageReport(client, CHECK_SUM_ERR, strPacket);
                    return;
                }

                //Size Check, UnKnown CMD Check
                string pktCheckCode = PacketCheck(ClientPacket);
                if (!pktCheckCode.Equals(OK_RET))
                {
                    ReceiveMessageLogWrite(ClientPacket, "", true); //$ 2023.01.05 : 테스트 무관하므로 true
                    HostUnknownMessageReport(client, pktCheckCode, strPacket);
                    return;
                }

                //EQP_ID Mapping
                if (this.BASE.DicClientEqpID.ContainsKey(client.Id))
                {
                    if (string.IsNullOrEmpty(this.BASE.DicClientEqpID[client.Id]))
                    {
                        this.BASE.DicClientEqpID[client.Id] = ClientPacket.ObjectID;
                        //Connect 후 설비측 CMD 925 대응이 안될경우 Connect 상태 변경을 여기서 진행 예정
                        ConnectionChange(ClientPacket);
                    }
                }
                else
                {
                    this.BASE.DicClientEqpID.Add(client.Id, ClientPacket.ObjectID);
                }

                //Cmd 처리
                MsgCallProcess(ClientPacket);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.SoketClient.Id);
            }
        }

        private void MsgCallProcess(CPacket ClientPacket)
        {
            switch (ClientPacket.RecvMgsID)
            {
                case "ELNK":  // As-Is : 925
                    /// <summary> 통신상태 보고 </summary>
                    EquipmentLinkCheck(ClientPacket);
                    break;
                case "EAPD":  // As-Is : 901, 903
                    /// <summary> 충방전기 실처리 Data 보고 </summary>
                    EquipmentActualProcessingData(ClientPacket);
                    break;
                case "ELSR":  // As-Is : 905
                    /// <summary> 충방전기 공정 시작/종료 </summary>
                    EquipmentLotStatusReport(ClientPacket);
                    break;
                case "EEER": // As-Is : 913, 910, 933, 937
                    /// <summary> 설비 상태 보고 </summary>
                    EquipmentEventReport(ClientPacket);
                    break;
                case "EALM":  // As-Is : 985, 975
                    /// <summary> 설비Trouble 보고 </summary>
                    EquipmentAlarmReport(ClientPacket);
                    break;
                case "ERSR":  // As-Is : 101
                    /// <summary> Recipe 요청 </summary>
                    EquipmentRecipeSpecRequest(ClientPacket);
                    break;
                case "ELIR":  // As-Is : 140
                    /// <summary> Channel 작업 여부 요청 </summary>
                    EquipmentLotInformationDataRequest(ClientPacket);
                    break;
                case "ECMD_R":  // As-Is : 138, 130
                    /// <summary> 설비 Tray Unload 지시 응답 </summary>
                    EquipmentCommandRecv(ClientPacket);
                    break;
                case "EUMR":  // As-Is : 981
                    /// <summary> Message Error</summary>
                    EquipmentUnknownMessageReport(ClientPacket);
                    break;
                case "EDNT_R":
                    EquipmentDateAndTimeSync(ClientPacket);
                    break;
                case "ESDR":    //$ 2024.03.19 : 화재 관련 보고용 신규 MessageID
                    /// <summary> 연기 감지 보고 </summary>
                    EquipmentSmokeDectectReport(ClientPacket);
                    break;
                default:
                    //Unknown CMD
                    break;
            }
        }

        private void ConnectionChange(CPacket ClientPacket)
        {
            int iRst = -1;
            String strBizName = "BR_SET_EIF_COMM_STATUS";
            Exception bizEx;

            try
            {
                /* bzActor Call */
                CBR_SET_EIF_COMM_STATUS_IN inData = CBR_SET_EIF_COMM_STATUS_IN.GetNew(this);
                CBR_SET_EIF_COMM_STATUS_OUT outData = CBR_SET_EIF_COMM_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = ClientPacket.ObjectID.TrimEnd();
                inData.IN_EQP[0].EIOCOMSTAT = eComStatus.ON.ToString();

                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx);
                if (iRst != 0)
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }

        #region Event Method
        #region Virtual Event Method   
        //$ 2023.05.16 : Nak Test가 Off될 경우 List를 Clear
        protected virtual void __TESTMODE_BIT_OnBooleanChanged(CVariable sender, bool value)
        {
            if (!this.BASE.TESTMODE__V_IS_NAK_TEST && !this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
            {
                if (this.NakPassList != null) this.NakPassList.Clear();
                if (this.TimeOutPassList != null) this.TimeOutPassList.Clear();
            }
        }
        #endregion
        #endregion

        #region Json Message Call Method
        #region ELNK : 통신상태 보고(ELNK - 925)
        protected virtual void EquipmentLinkCheck(CPacket ClientPacket)
        {
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                ReceiveMessageLogWrite(ClientPacket, txnID, true); //$ 2023.05.15 : 테스트 무관하므로 true

                ClientPacket.SendBodyList.Add("1", "{}");
                SendReplyMsg(ClientPacket, txnID);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);

                ClientPacket.SendBodyList.Add("1", "{}");
                SendReplyMsg(ClientPacket, txnID);
            }
            finally
            {
                try
                {
                    //$ 2021.03.30 : 설비에서 ELNK를 요청시 시간 동기화를 요청, 설비가 재시작 될 경우 외에 ELNK는 주지 않으니..
                    DateTime dateNow = DateTime.Now;

                    #region 시간 동기화 요청
                    CEDNT ednt = new CEDNT { EqpID = this.HostObjID, Time = dateNow.ToString("yyyyMMddHHmmss") };
                    string msgID = "EDNT";
                    JObject json = new JObject();
                    json.Add(msgID, JObject.FromObject(ednt));

                    SendHostMsg(ClientPacket.SoketClient, "", msgID, EX_REPLY, json.ToString());
                    #endregion
                }
                catch (Exception ex)
                {
                    _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                }
            }
        }
        #endregion

        #region EAPD : 실처리 Data 보고(EAPD - 901)
        protected virtual void EquipmentActualProcessingData(CPacket ClientPacket)
        {
            Exception bizEx = null;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                int iRst = -1;
                string sRet = EXCEPTION_ERR_RET;
                string hostMsg = string.Empty;
                string troubleCd = string.Empty;
                String strBizName = string.Empty;

                bool isTimeout = ReceiveMessageLogWrite(ClientPacket, txnID);
                if (isTimeout) return;

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];

                eOperGroup Opergroup = (eOperGroup)(Enum.Parse(typeof(eOperGroup), msgObject["OPERGROUPID"].ToString()));
                string reportType = msgObject["REPORTTYPE"].ToString();

                switch (Opergroup)
                {
                    case eOperGroup.CHARGE:
                    case eOperGroup.DISCHARGE:
                        {
                            #region 충방전 실처리 + CCCV 보고
                            if (reportType == "P")
                            {
                                #region 충방전 실처리
                                strBizName = "BR_SET_FORM_APD_REPORT_CHG";
                                CBR_SET_FORM_APD_REPORT_CHG_IN inData = CBR_SET_FORM_APD_REPORT_CHG_IN.GetNew(this);
                                CBR_SET_FORM_APD_REPORT_CHG_OUT outData = CBR_SET_FORM_APD_REPORT_CHG_OUT.GetNew(this);
                                inData.INDATA_LENGTH = 0;

                                CEAPD eapd = JsonConvert.DeserializeObject<CCharge_EAPD>(msgObject.ToString());
                                var TrayInfo = (eapd as CCharge_EAPD);

                                int i = 0;

                                foreach (CCharge_Info resultInfo in TrayInfo.Charge_Info)
                                {
                                    foreach (var cellInfo in resultInfo.ChargeDataArr)
                                    {
                                        inData.INDATA_LENGTH++;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = TrayInfo.TrayIDs[i];
                                        inData.INDATA[inData.INDATA_LENGTH - 1].PROCID = ((int)Opergroup).ToString();
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTSLOT = cellInfo.CellNO;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].GOOD_NG_INFO = cellInfo.CellGrade;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].PROC_STEP_NO = cellInfo.StepCount;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CAPA_VALUE = cellInfo.Capacity;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].AVG_VLTG_VALUE = cellInfo.AVGVoltage;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].END_VLTG_VALUE = cellInfo.EndVoltage;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].END_CURNT_VALUE = cellInfo.Current;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].DFCT_CODE = cellInfo.ErrorCode;
                                    }

                                    i++;
                                }

                                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                                if (iRst != 0 || outData == null)
                                {
                                    if (iRst != 0)
                                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                }
                                else
                                    sRet = outData.OUTDATA[0].RETVAL.ToString();
                                #endregion
                            }
                            else
                            {
                                #region CCCV 보고
                                strBizName = "BR_SET_FORM_CCCV_REPORT";
                                CBR_SET_FORM_CCCV_REPORT_IN inData = CBR_SET_FORM_CCCV_REPORT_IN.GetNew(this);
                                CBR_SET_FORM_CCCV_REPORT_OUT outData = CBR_SET_FORM_CCCV_REPORT_OUT.GetNew(this);
                                inData.INDATA_LENGTH = 0;

                                CEAPD eapd = JsonConvert.DeserializeObject<CCCCV_EAPD>(msgObject.ToString());
                                var TrayInfo = (eapd as CCCCV_EAPD);

                                int i = 0;

                                foreach (CCCCV_Info resultInfo in TrayInfo.CCCV_Info)
                                {
                                    foreach (var cellInfo in resultInfo.CCCVDataArr)
                                    {
                                        inData.INDATA_LENGTH++;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = TrayInfo.TrayIDs[i];

                                        //inData.INDATA[inData.INDATA_LENGTH - 1].PROC_GROUP_ID = Opergroup.ToString();  //$ 2020.12.01 : 우총괄님께서 String으로 보내달라고 요청하셔서 수정
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTSLOT = cellInfo.CellNO;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].GOOD_NG_INFO = cellInfo.CellGrade;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].PROC_STEP_NO = cellInfo.StepCount;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CCURNT_CAPA_VALUE = cellInfo.CCCapacity;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CCURNT_TIME = cellInfo.CCProcTime;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CVLTG_CAPA_VALUE = cellInfo.CVCapacity;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CVLTG_TIME = cellInfo.CVProcTime;
                                    }

                                    i++;  //$ 2021.04.20 : i 증가가 되지 않아서 TrayID가 하단으로만 보고 되어 추가
                                }

                                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                                if (iRst != 0 || outData == null)
                                {
                                    if (iRst != 0)
                                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                }
                                else
                                    sRet = outData.OUTDATA[0].RETVAL.ToString();
                                #endregion
                            }
                            #endregion
                        }
                        break;

                    case eOperGroup.OCV:
                        {
                            #region OCV 실처리
                            strBizName = "BR_SET_FORM_APD_REPORT_OCV";
                            CBR_SET_FORM_APD_REPORT_OCV_IN inData = CBR_SET_FORM_APD_REPORT_OCV_IN.GetNew(this);
                            CBR_SET_FORM_APD_REPORT_OCV_OUT outData = CBR_SET_FORM_APD_REPORT_OCV_OUT.GetNew(this);
                            inData.INDATA_LENGTH = 0;

                            CEAPD eapd = JsonConvert.DeserializeObject<COCV_EAPD>(msgObject.ToString());
                            var TrayInfo = (eapd as COCV_EAPD);

                            int i = 0;

                            foreach (COCV_Info resultInfo in TrayInfo.OCV_Info)
                            {
                                foreach (var cellInfo in resultInfo.OCVDataArr)
                                {
                                    inData.INDATA_LENGTH++;

                                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;

                                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = TrayInfo.TrayIDs[i];
                                    inData.INDATA[inData.INDATA_LENGTH - 1].PROCID = ((int)Opergroup).ToString();
                                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTSLOT = cellInfo.CellNO;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].GOOD_NG_INFO = cellInfo.CellGrade;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].AVG_VLTG_VALUE = cellInfo.AVGVoltage;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].DFCT_CODE = cellInfo.ErrorCode;
                                }

                                i++;
                            }

                            iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                            if (iRst != 0 || outData == null)
                            {
                                if (iRst != 0)
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                                _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                            }
                            else
                                sRet = outData.OUTDATA[0].RETVAL.ToString();

                            #endregion
                        }
                        break;

                    case eOperGroup.IMP:
                        {
                            #region IMP 실처리
                            //$ 2020.07.28 : IMP를 써본적이 없고 정확한 내역이 불명확하여 일단 좌측 2개로 등록 추후 IMP사용안한다면 삭제하던가 하자!!
                            //CSET_FORM_APD_REPORT_IMP_IN inData = CSET_FORM_APD_REPORT_IMP_IN.GetNew(this);
                            //CSET_FORM_APD_REPORT_IMP_OUT outData = CSET_FORM_APD_REPORT_IMP_OUT.GetNew(this);
                            //inData.INDATA_LENGTH = 0;

                            //foreach (var trayInfo in message.TrayList)
                            //{
                            //    foreach (var cellInfo in trayInfo.CellList.OrderBy(x => x.APD.CELL_No))
                            //    {
                            //        C901Message.CIMP apd = cellInfo.APD as C901Message.CIMP;
                            //        inData.INDATA_LENGTH++;
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].TRAY_ID = trayInfo.TrayID;
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].OP_ID = ((int)eOpGroup).ToString();
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].CELL_NO = apd.CELL_No;
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].GOOD_NG_INFO = apd.GOOD_NG_INFO;
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].IMP_VAL = apd.IMP_VAL;
                            //        inData.INDATA[inData.INDATA_LENGTH - 1].BAD_TYPE_CD = apd.BAD_TYPE_CD;
                            //    }
                            //}
                            ///* BizCall Call */
                            //iRst = FAService.Request("SET_FORM_APD_REPORT_IMP", inData.Variable, outData.Variable);
                            //if (iRst != 0 || outData == null)
                            //{
                            //    ClientPacket.SendBodyList.Add("1", EXCEPTION_ERR_RET);
                            //    SendReplyCmd(ClientPacket);
                            //    return;
                            //}
                            //sRet = outData.OUTDATA[0].RETVAL.ToString();
                            #endregion
                        }
                        break;

                    case eOperGroup.POWERCHG:
                    case eOperGroup.POWERGRD:
                        {
                            #region MEGA 충방전 실처리
                            strBizName = "BR_SET_FORM_APD_REPORT_MEGA";
                            CBR_SET_FORM_APD_REPORT_MEGA_IN inData = CBR_SET_FORM_APD_REPORT_MEGA_IN.GetNew(this);
                            CBR_SET_FORM_APD_REPORT_MEGA_OUT outData = CBR_SET_FORM_APD_REPORT_MEGA_OUT.GetNew(this);
                            inData.INDATA_LENGTH = 0;

                            CEAPD eapd = JsonConvert.DeserializeObject<CPower_EAPD>(msgObject.ToString());
                            var TrayInfo = (eapd as CPower_EAPD);

                            int i = 0;

                            foreach (CPower_Info resultInfo in TrayInfo.Power_Info)
                            {
                                foreach (var cellInfo in resultInfo.PowerDataArr)
                                {
                                    inData.INDATA_LENGTH++;

                                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = TrayInfo.TrayIDs[i];
                                    inData.INDATA[inData.INDATA_LENGTH - 1].PROCID = ((int)Opergroup).ToString();
                                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTSLOT = cellInfo.CellNO;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].GOOD_NG_INFO = cellInfo.CellGrade;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].DCIR_VALUE = cellInfo.Impedance;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].STRT_VLTG_VALUE = cellInfo.StartVoltage;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].END_VLTG_VALUE = cellInfo.EndVoltage;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].END_CURNT_VALUE = cellInfo.Current;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].DFCT_CODE = cellInfo.ErrorCode;
                                }

                                i++;
                            }

                            iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                            if (iRst != 0 || outData == null)
                            {
                                if (iRst != 0)
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                                _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                            }
                            else
                                sRet = outData.OUTDATA[0].RETVAL.ToString();
                            #endregion
                        }
                        break;

                    default:
                        hostMsg = _EIFServer.ExceptionMessage("EAPD Unknow OperGroup Host Biz Error");
                        sRet = EXCEPTION_ERR_RET;
                        break;
                }

                if (!(sRet.Equals(OK_RET) || sRet.Equals(EXCEPTION_ERR_RET))) sRet = NG_RET;

                EquipmentActualProcessingDataReply(ClientPacket, txnID, sRet, hostMsg);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentActualProcessingDataReply(ClientPacket, txnID, new CEAPD_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("EAPD HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentActualProcessingDataReply(CPacket ClientPacket, string txnID, string ret, string hostMsg)
        {
            CEAPD_R eapd_r = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                string dataSim = _EIFServer.GetJsonValue(typeof(CEAPD_R));
                eapd_r = JsonConvert.DeserializeObject<CEAPD_R>(dataSim);

                if (eapd_r == null) throw new Exception($"[{eapd_r.ToString()}] : Simulation Data Not Exist!!");

                eapd_r.EqpID = ClientPacket.ObjectID; // EqpID 경우 실제 Socket 연결된 데이터가 더 정확하여 이를 사용, 그 외에 값은 Simul 데이터 사용, 필요시 주석 해제해서 인자로도 전송 가능
                //eapd_r.ACK = ret;         
                //eapd_r.HostMSG = hostMsg;
                #endregion
            }
            else
            {
                #region Real Case
                eapd_r = new CEAPD_R { EqpID = ClientPacket.ObjectID, ACK = ret, HostMSG = hostMsg };
                #endregion
            }

            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(eapd_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID);
        }

        protected virtual void EquipmentActualProcessingDataReply(CPacket ClientPacket, string txnID, CEAPD_R eapd_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(eapd_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false);
        }
        #endregion

        #region ELSR : 공정 시작/종료(ELSR - 905)
        protected virtual void EquipmentLotStatusReport(CPacket ClientPacket)
        {
            int iRst = -1;
            string hostMsg = string.Empty;
            string troubleCd = string.Empty;

            string sRet = EXCEPTION_ERR_RET;
            string sNextProcess = string.Empty;
            string sProfileYN = string.Empty;
            string sStartTime = string.Empty;
            string sLowerTrayid = string.Empty;
            string sUpperTrayid = string.Empty;
            string sOperid = string.Empty;
            String strBizName = string.Empty;
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                bool isTimeout = ReceiveMessageLogWrite(ClientPacket, txnID);
                if (isTimeout) return;

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];
                CELSR elsr = JsonConvert.DeserializeObject<CELSR>(msgObject.ToString());

                if (elsr.LotState.Equals("S"))
                {
                    #region 공정시작
                    strBizName = "BR_SET_FORM_TRAY_START";
                    CBR_SET_FORM_TRAY_START_IN inData = CBR_SET_FORM_TRAY_START_IN.GetNew(this);
                    CBR_SET_FORM_TRAY_START_OUT outData = CBR_SET_FORM_TRAY_START_OUT.GetNew(this);
                    inData.INDATA_LENGTH = 0;

                    foreach (var trayID in elsr.TrayIDs)
                    {
                        if (string.IsNullOrEmpty(trayID) || string.IsNullOrWhiteSpace(trayID)) continue;  // trayid null, space일 경우 경우에 뺀다

                        inData.INDATA_LENGTH++;

                        inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                        inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                        inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                        inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;
                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                        inData.INDATA[inData.INDATA_LENGTH - 1].CST_LOAD_LOCATION_CODE = inData.INDATA_LENGTH.ToString();
                    }

                    iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                    if (iRst != 0 || outData == null)
                    {
                        if (iRst != 0)
                            _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                        _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                    }
                    else
                    {
                        sRet = outData.OUTDATA[0].RETVAL.ToString();
                        sNextProcess = outData.OUTDATA[0].NEXTPROCESS;
                        sProfileYN = outData.OUTDATA[0].PROFILE_YN;
                    }
                    #endregion
                }
                else
                {
                    #region "공정종료"
                    strBizName = "BR_SET_FORM_TRAY_END";
                    CBR_SET_FORM_TRAY_END_IN inData = CBR_SET_FORM_TRAY_END_IN.GetNew(this);
                    CBR_SET_FORM_TRAY_END_OUT outData = CBR_SET_FORM_TRAY_END_OUT.GetNew(this);
                    inData.INDATA_LENGTH = 0;
                    inData.TMPR_DATA_START_LENGTH = 0;
                    inData.TMPR_DATA_AVG_LENGTH = 0;
                    inData.TMPR_DATA_MIN_LENGTH = 0;
                    inData.TMPR_DATA_MAX_LENGTH = 0;
                    inData.TMPR_DATA_END_LENGTH = 0;

                    int i = 0;
                    foreach (var trayID in elsr.TrayIDs)
                    {
                        if (string.IsNullOrEmpty(trayID) || string.IsNullOrWhiteSpace(trayID)) continue;  // trayid null, space일 경우 경우에 뺀다

                        #region 공정 종료 시 온도 보고
                        inData.INDATA_LENGTH++;
                        inData.INDATA[i].SRCTYPE = SRCTYPE.EQUIPMENT;
                        inData.INDATA[i].IFMODE = IFMODE.ONLINE;
                        inData.INDATA[i].USERID = USERID.EIF;

                        inData.INDATA[i].EQPTID = ClientPacket.ObjectID;
                        inData.INDATA[i].CSTID = trayID;

                        //초기온도값
                        inData.TMPR_DATA_START_LENGTH++;
                        inData.TMPR_DATA_START[i].TMPR1_VALUE = elsr.BoxTempList[$"{i + 1}_START_01"];
                        inData.TMPR_DATA_START[i].TMPR2_VALUE = elsr.BoxTempList[$"{i + 1}_START_02"];
                        inData.TMPR_DATA_START[i].TMPR3_VALUE = elsr.BoxTempList[$"{i + 1}_START_03"];
                        inData.TMPR_DATA_START[i].TMPR4_VALUE = elsr.BoxTempList[$"{i + 1}_START_04"];
                        inData.TMPR_DATA_START[i].TMPR5_VALUE = elsr.BoxTempList[$"{i + 1}_START_05"];
                        inData.TMPR_DATA_START[i].TMPR6_VALUE = elsr.BoxTempList[$"{i + 1}_START_06"];
                        inData.TMPR_DATA_START[i].TMPR7_VALUE = elsr.BoxTempList[$"{i + 1}_START_07"];
                        inData.TMPR_DATA_START[i].TMPR8_VALUE = elsr.BoxTempList[$"{i + 1}_START_08"];
                        inData.TMPR_DATA_START[i].TMPR9_VALUE = elsr.BoxTempList[$"{i + 1}_START_09"];
                        inData.TMPR_DATA_START[i].TMPR10_VALUE = elsr.BoxTempList[$"{i + 1}_START_10"];
                        inData.TMPR_DATA_START[i].TMPR11_VALUE = elsr.BoxTempList[$"{i + 1}_START_11"];
                        inData.TMPR_DATA_START[i].TMPR12_VALUE = elsr.BoxTempList[$"{i + 1}_START_12"];
                        inData.TMPR_DATA_START[i].TMPR13_VALUE = elsr.BoxTempList[$"{i + 1}_START_13"];

                        //평균 온도값
                        inData.TMPR_DATA_AVG_LENGTH++;
                        inData.TMPR_DATA_AVG[i].TMPR1_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_01"];
                        inData.TMPR_DATA_AVG[i].TMPR2_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_02"];
                        inData.TMPR_DATA_AVG[i].TMPR3_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_03"];
                        inData.TMPR_DATA_AVG[i].TMPR4_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_04"];
                        inData.TMPR_DATA_AVG[i].TMPR5_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_05"];
                        inData.TMPR_DATA_AVG[i].TMPR6_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_06"];
                        inData.TMPR_DATA_AVG[i].TMPR7_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_07"];
                        inData.TMPR_DATA_AVG[i].TMPR8_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_08"];
                        inData.TMPR_DATA_AVG[i].TMPR9_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_09"];
                        inData.TMPR_DATA_AVG[i].TMPR10_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_10"];
                        inData.TMPR_DATA_AVG[i].TMPR11_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_11"];
                        inData.TMPR_DATA_AVG[i].TMPR12_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_12"];
                        inData.TMPR_DATA_AVG[i].TMPR13_VALUE = elsr.BoxTempList[$"{i + 1}_AVG_13"];

                        //최소 온도값
                        inData.TMPR_DATA_MIN_LENGTH++;
                        inData.TMPR_DATA_MIN[i].TMPR1_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_01"];
                        inData.TMPR_DATA_MIN[i].TMPR2_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_02"];
                        inData.TMPR_DATA_MIN[i].TMPR3_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_03"];
                        inData.TMPR_DATA_MIN[i].TMPR4_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_04"];
                        inData.TMPR_DATA_MIN[i].TMPR5_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_05"];
                        inData.TMPR_DATA_MIN[i].TMPR6_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_06"];
                        inData.TMPR_DATA_MIN[i].TMPR7_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_07"];
                        inData.TMPR_DATA_MIN[i].TMPR8_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_08"];
                        inData.TMPR_DATA_MIN[i].TMPR9_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_09"];
                        inData.TMPR_DATA_MIN[i].TMPR10_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_10"];
                        inData.TMPR_DATA_MIN[i].TMPR11_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_11"];
                        inData.TMPR_DATA_MIN[i].TMPR12_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_12"];
                        inData.TMPR_DATA_MIN[i].TMPR13_VALUE = elsr.BoxTempList[$"{i + 1}_MIN_13"];

                        //최대 온도값
                        inData.TMPR_DATA_MAX_LENGTH++;
                        inData.TMPR_DATA_MAX[i].TMPR1_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_01"];
                        inData.TMPR_DATA_MAX[i].TMPR2_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_02"];
                        inData.TMPR_DATA_MAX[i].TMPR3_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_03"];
                        inData.TMPR_DATA_MAX[i].TMPR4_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_04"];
                        inData.TMPR_DATA_MAX[i].TMPR5_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_05"];
                        inData.TMPR_DATA_MAX[i].TMPR6_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_06"];
                        inData.TMPR_DATA_MAX[i].TMPR7_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_07"];
                        inData.TMPR_DATA_MAX[i].TMPR8_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_08"];
                        inData.TMPR_DATA_MAX[i].TMPR9_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_09"];
                        inData.TMPR_DATA_MAX[i].TMPR10_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_10"];
                        inData.TMPR_DATA_MAX[i].TMPR11_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_11"];
                        inData.TMPR_DATA_MAX[i].TMPR12_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_12"];
                        inData.TMPR_DATA_MAX[i].TMPR13_VALUE = elsr.BoxTempList[$"{i + 1}_MAX_13"];

                        //종료 온도값
                        inData.TMPR_DATA_END_LENGTH++;
                        inData.TMPR_DATA_END[i].TMPR1_VALUE = elsr.BoxTempList[$"{i + 1}_END_01"];
                        inData.TMPR_DATA_END[i].TMPR2_VALUE = elsr.BoxTempList[$"{i + 1}_END_02"];
                        inData.TMPR_DATA_END[i].TMPR3_VALUE = elsr.BoxTempList[$"{i + 1}_END_03"];
                        inData.TMPR_DATA_END[i].TMPR4_VALUE = elsr.BoxTempList[$"{i + 1}_END_04"];
                        inData.TMPR_DATA_END[i].TMPR5_VALUE = elsr.BoxTempList[$"{i + 1}_END_05"];
                        inData.TMPR_DATA_END[i].TMPR6_VALUE = elsr.BoxTempList[$"{i + 1}_END_06"];
                        inData.TMPR_DATA_END[i].TMPR7_VALUE = elsr.BoxTempList[$"{i + 1}_END_07"];
                        inData.TMPR_DATA_END[i].TMPR8_VALUE = elsr.BoxTempList[$"{i + 1}_END_08"];
                        inData.TMPR_DATA_END[i].TMPR9_VALUE = elsr.BoxTempList[$"{i + 1}_END_09"];
                        inData.TMPR_DATA_END[i].TMPR10_VALUE = elsr.BoxTempList[$"{i + 1}_END_10"];
                        inData.TMPR_DATA_END[i].TMPR11_VALUE = elsr.BoxTempList[$"{i + 1}_END_11"];
                        inData.TMPR_DATA_END[i].TMPR12_VALUE = elsr.BoxTempList[$"{i + 1}_END_12"];
                        inData.TMPR_DATA_END[i].TMPR13_VALUE = elsr.BoxTempList[$"{i + 1}_END_13"];
                        #endregion

                        i++;
                    }

                    iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                    if (iRst != 0 || outData == null)
                    {
                        if (iRst != 0)
                            _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                        //$ 2023.12.19 : Biz 실행 시 DEADLOCK으로 Exception이 발생 할 경우 1회 Reply를 Skip하게 하여 20s 후 설비에서 재 보고 처리 가능하도록 변경
                        if (bizEx.Message.ToUpper().Contains("DEADLOCK"))
                        {
                            if (!_EIFServer.CheckBeforeDeadLockFlag(ClientPacket.ObjectID))
                            {
                                _EIFServer.SetLog("ELSR DeadLock Occur!! Skip once", this.HostObjID, ClientPacket.ObjectID);
                                return;
                            }
                        }

                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송

                        _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);

                        _EIFServer.SetLog("ELSR Host Biz Error", this.HostObjID, ClientPacket.ObjectID);
                    }
                    else
                    {
                        sRet = outData.OUTDATA[0].RETVAL.ToString();
                        sNextProcess = outData.OUTDATA[0].NEXT_PROCESS;
                        sProfileYN = outData.OUTDATA[0].PROFILE_YN;
                    }
                    #endregion
                }

                if (!(sRet.Equals(OK_RET) || sRet.Equals(DIRECTION_ERROR_RET) || sRet.Equals(EXCEPTION_ERR_RET)))
                    sRet = NG_RET;

                EquipmentLotStatusReportReply(ClientPacket, txnID, sRet, sNextProcess, sProfileYN, elsr.TrayIDs, hostMsg, elsr.LotState);

                //$ 2023.12.19 : Biz Exception Flag를 초기화 하여 신규 Tray에서 Deadlock 발생 시 한번은 막자
                _EIFServer.ResetDeadLockFlag(ClientPacket.ObjectID);

            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentLotStatusReportReply(ClientPacket, txnID, new CELSR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("ELSR HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentLotStatusReportReply(CPacket ClientPacket, string txnID, string ret, string nextProcess, string profileYN, string[] trayIds, string hostMsg, string lotState, string startTime = "", string operId = "")
        {
            // ProfileYN의 값에 Y일 경우 startTime과 operId를 인자로 전달해야 하나 현재 Biz에서 전달되는게 없는 상태라 default 인자로만 선언해 둠

            CELSR_R elsr_r = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                //$ 2024.05.30 : Source 가독성을 위해 위치 변경
                //착공의 경우 NextProcess는 E, 완공의 경우 최대 배열 값이 될 경우 N, 아직 Step이 남은 경우 E
                nextProcess = "E";
                if (lotState.Equals("E"))
                {
                    this.ContiStep++;
                    int maxStep = (this.EQPINFO__V_IS_POWER_BOX) ? this.m_ArrContPowWorks.Length - 1 : this.m_ArrContWorks.Length - 1;

                    if (this.ContiStep > maxStep)
                    {
                        nextProcess = "N";
                        this.ContiStep = 0;
                    }
                }

                string dataSim = _EIFServer.GetJsonValue(typeof(CELSR_R));
                elsr_r = JsonConvert.DeserializeObject<CELSR_R>(dataSim);

                if (elsr_r == null) throw new Exception($"[{elsr_r.ToString()}] : Simulation Data Not Exist!!");

                elsr_r.EqpID = ClientPacket.ObjectID; // EqpID 경우 실제 Socket 연결된 데이터가 더 정확하여 이를 사용, 그 외에 값은 Simul 데이터 사용, 필요시 주석 해제해서 인자로도 전송 가능
                //elsr_r.ACK = ret;
                elsr_r.NextProcess = nextProcess;

                JObject joSim = JObject.Parse(dataSim);
                string subData = joSim["LSD"].ToString();
                CBoxLSD tokenData = JsonConvert.DeserializeObject<CBoxLSD>(subData);
                elsr_r.LSD = tokenData;
                (elsr_r.LSD as CBoxLSD).TrayIDs = trayIds;
                #endregion
            }
            else
            {
                #region Real Case
                elsr_r = new CELSR_R { EqpID = ClientPacket.ObjectID, ACK = ret, NextProcess = nextProcess, HostMSG = hostMsg };

                if (ret != EXCEPTION_ERR_RET) //Host Error 9인 경우 LSD는 null로 이외에 조건도 해당되면 조건에 포함
                {
                    //$ 2020.08.18 : Profile정보 아래단은 실제 OutData가 없음. 결국 설비로 내려줄께 없는데.. 일단 하기처럼 전달 할 수 있는 구조는 만들어 둠
                    if (profileYN == "0")
                        elsr_r.LSD = new CBoxLSD() { ProfileGather = "N", StartTime = "", OperID = "", TrayIDs = trayIds };
                    else
                        elsr_r.LSD = new CBoxLSD() { ProfileGather = profileYN, StartTime = startTime, OperID = operId, TrayIDs = trayIds };
                }
                #endregion
            }

            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(elsr_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID);
        }

        protected virtual void EquipmentLotStatusReportReply(CPacket ClientPacket, string txnID, CELSR_R elsr_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(elsr_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false);
        }
        #endregion


        #region EEER : 설비 상태 보고(913), 작업 Mode 보고(917), 작업 Type 변경 보고 응답(919), Tray LD/ULD(933, 937), 작업 Type 변경 보고(919), 온도 보고(909) (EEER)
        protected virtual void EquipmentEventReport(CPacket ClientPacket)
        {
            int iRst = -1;
            string hostMsg = string.Empty;
            string troubleCd = string.Empty;
            string ceid = string.Empty;
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];

                ceid = msgObject["CEID"].ToString();
                string replyInfo = string.Empty;
                String strBizName = string.Empty;

                if (ceid != CEID._651 || (ceid == CEID._651 && this.BASE.TESTMODE__V_IS_TEMPLOG_USE)) //$ 2023.06.03 : CEID 651 Log 사용 여부 옵션처리, Reply는 남김
                {
                    bool isTimeout = ReceiveMessageLogWrite(ClientPacket, txnID, false, ceid);
                    if (isTimeout) return;
                }
                else if (ceid == CEID._651 && !this.BASE.TESTMODE__V_IS_TEMPLOG_USE)
                {
                    string msgID = string.IsNullOrEmpty(ceid) ? ClientPacket.RecvMgsID : $"{ClientPacket.RecvMgsID} {ceid}";
                    SolaceLog(ClientPacket.ObjectID, txnID, 1, $"RECV : {msgID} - {ClientPacket.Dir + ClientPacket.RawObjectID + ClientPacket.Type + ClientPacket.Reply + ClientPacket.MsgID + ClientPacket.SeqNo}");
                }

                CEEER eeer = null;
                switch (ceid)
                {
                    case CEID._601:
                        {
                            #region CEID 601 [917]
                            eeer = JsonConvert.DeserializeObject<COP_Mode_EEER>(msgObject.ToString());
                            var eqInfo = (eeer as COP_Mode_EEER);

                            strBizName = "BR_SET_EQP_MAINT_FLAG";

                            //$ 2023.12.19 : 최완영 추가  설비모드 운영 변경건
                            CBR_SET_EQP_MAINT_FLAG_IN inData = CBR_SET_EQP_MAINT_FLAG_IN.GetNew(this);
                            CBR_SET_EQP_MAINT_FLAG_OUT outData = CBR_SET_EQP_MAINT_FLAG_OUT.GetNew(this);
                            inData.IN_EQP_LENGTH = 1;

                            inData.IN_EQP[inData.IN_EQP_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData.IN_EQP[inData.IN_EQP_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                            inData.IN_EQP[inData.IN_EQP_LENGTH - 1].USERID = USERID.EIF;

                            inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPTID = eqInfo.EqpID;
                            inData.IN_EQP[inData.IN_EQP_LENGTH - 1].EQPT_MAINT_FLAG = eqInfo.Operation_Mode.OperMode;

                            iRst = BizCall(strBizName, eqInfo.EqpID, inData, outData, out bizEx, txnID);
                            if (iRst != 0 || outData == null)
                            {
                                if (iRst != 0)
                                {
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                }

                                iRst = 9;
                                _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                            }
                            #endregion
                        }
                        break;
                    case CEID._605:
                        {
                            #region CEID 605  [919]  
                            eeer = JsonConvert.DeserializeObject<COP_Type_EEER>(msgObject.ToString());
                            var opType = (eeer as COP_Type_EEER);

                            strBizName = "BR_SET_FORM_TYPE_INFO";
                            CBR_SET_FORM_TYPE_INFO_IN inData = CBR_SET_FORM_TYPE_INFO_IN.GetNew(this);
                            CBR_SET_FORM_TYPE_INFO_OUT outData = CBR_SET_FORM_TYPE_INFO_OUT.GetNew(this);
                            inData.INDATA_LENGTH = 1;

                            inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                            inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                            inData.INDATA[0].EQPTID = opType.EqpID;
                            inData.INDATA[0].TYPE_INFO = opType.Operation_Type.OperType;

                            iRst = BizCall(strBizName, opType.EqpID, inData, outData, out bizEx, txnID);
                            if (iRst != 0 || outData == null)
                            {
                                if (iRst != 0)
                                {
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                }

                                iRst = 9;
                                _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                            }
                            #endregion
                        }
                        break;
                    case CEID._611:
                        {
                            #region CEID 611 [913]
                            eeer = JsonConvert.DeserializeObject<CEqp_State_EEER>(msgObject.ToString());
                            var eqInfo = (eeer as CEqp_State_EEER);

                            string sCode = eqInfo.Eqp_State.EqpState;
                            _EIFServer.SetLog($"EqpStatus : {sCode}", this.HostObjID, ClientPacket.ObjectID);

                            eEqpStatus state = eEqpStatus.W;
                            string subState = eqInfo.Eqp_State.EqpSubState;

                            switch (sCode)
                            {
                                //R, I->W, T, M->U(SubState 181), S->U(특정 SubState), P(없애고 바로 다음 상태로(W나 R)), O->F
                                case "R":  //RUN
                                case "T":  //TROUBLE
                                    state = (eEqpStatus)Enum.Parse(typeof(eEqpStatus), sCode, true);
                                    break;

                                case "I":  //IDLE
                                case "P": // POWER ON
                                    state = (eEqpStatus)Enum.Parse(typeof(eEqpStatus), "W", true);
                                    break;

                                case "S":  //PAUSE
                                    state = (eEqpStatus)Enum.Parse(typeof(eEqpStatus), "U", true);
                                    subState = "181";  //2021.03.11 정종덕 책임님과 협의하여 일시정지는 UserStop, 181로 정의하기로 함.
                                    break;

                                case "O": // POWER OFF
                                    state = (eEqpStatus)Enum.Parse(typeof(eEqpStatus), "F", true);
                                    break;

                                case "M": // MAINT
                                    state = (eEqpStatus)Enum.Parse(typeof(eEqpStatus), "U", true);
                                    if (string.IsNullOrEmpty(subState)) subState = "111"; //설비에서 Maint인데 SubState가 없는 경우 111, 값이 있다면 해당 값을(아마도 113이 주가 될듯..)
                                    break;

                                default:
                                    _EIFServer.SetLog($"EqpStatus : {sCode} => Not Regist", this.HostObjID, ClientPacket.ObjectID);
                                    break;
                            }

                            strBizName = "BR_SET_EQP_STATUS";
                            CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);
                            inData.IN_EQP_LENGTH = 1;

                            inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData.IN_EQP[0].USERID = USERID.EIF;

                            inData.IN_EQP[0].EQPTID = eqInfo.EqpID;
                            inData.IN_EQP[0].EIOSTAT = state.ToString();

                            //$ 2023.07.07 : Substate 값이 100보다 작은 경우 0으로 치환, 숫자로 전환이 안되는 경우 보고 하지 않음
                            ushort uSubStatus = 0;
                            if (ushort.TryParse(subState, out uSubStatus))
                            {
                                if (this.EQPINFO__V_IS_SIXLOSSCODE_USE) //$ 2023.07.26 : Loss Code 6자리 사용 시
                                {
                                    if (uSubStatus == 181)
                                        inData.IN_EQP[0].LOSS_CODE = uSubStatus.ToString(); //$ 2023.07.26 : 181은 Loss Code가 아니고 일시정지에 대한 Key값임, 이걸 바꾸려면 MMD 공통코드관리 FORMEQPT_PAUSE에서 속성 3값을 변경해야 함
                                    else
                                        inData.IN_EQP[0].LOSS_CODE = uSubStatus < 100 ? "000000" : uSubStatus.ToString("D6"); //$ 2023.07.13 : 표준화팀 요청 LOSS Code 6자리 변경
                                }
                                else
                                {
                                    inData.IN_EQP[0].LOSS_CODE = uSubStatus < 100 ? "0" : uSubStatus.ToString(); //$ 2023.07.25 :  MMD 반영 전까지는 기촌 처럼 운영
                                }
                            }

                            //913(T) 인 경우, 985로 대체하고 별도 처리하지 않음
                            if (state == eEqpStatus.T)
                                iRst = 0;
                            else
                            {
                                iRst = BizCall(strBizName, eqInfo.EqpID, inData, outData, out bizEx, txnID);
                                if (iRst != 0 || outData == null)
                                {
                                    if (iRst != 0)
                                    {
                                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                    }

                                    iRst = 9;
                                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                }
                            }
                            #endregion
                        }
                        break;
                    case CEID._621:
                        {
                            #region CEID 621 [933, 937]                                                        
                            eeer = JsonConvert.DeserializeObject<CPort_State_EEER>(msgObject.ToString());
                            var portInfo = (eeer as CPort_State_EEER);

                            //LR : Load 요청, UR : Unload 요청,
                        LI: Loading 중, UI : Unloading 중, LC: Load 완료, UC : Unload 완료
                            ePortState portState = portInfo.Port_State.PortState;
                            replyInfo = portState.ToString();

                            if (portState == ePortState.UC)
                            {
                                #region 공정 처리 : UC에서 온도 보고 필요
                                // As-Is의 경우 Unload보고와 온도 데이터 보고가 같이 진행
                                // To-Be의 경우 Unload보고는 MHS 관련 Biz에서 진행 온도 보고만을 위해 하기 Logic 수행됨

                                strBizName = "BR_SET_FORM_UNLOAD_END";
                                CBR_SET_FORM_UNLOAD_END_IN inData = CBR_SET_FORM_UNLOAD_END_IN.GetNew(this);
                                CBR_SET_FORM_UNLOAD_END_OUT outData = CBR_SET_FORM_UNLOAD_END_OUT.GetNew(this);
                                inData.INDATA_LENGTH = 0;

                                foreach (var trayID in portInfo.Port_State.TrayIDs)
                                {
                                    if (string.IsNullOrEmpty(trayID)) continue;

                                    inData.INDATA_LENGTH++;

                                    inData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                                    inData.INDATA[0].IFMODE = IFMODE.ONLINE;
                                    inData.INDATA[0].USERID = USERID.EIF;

                                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = portInfo.EqpID;
                                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                                }

                                iRst = BizCall(strBizName, portInfo.EqpID, inData, outData, out bizEx, txnID);
                                if (iRst != 0 || outData == null)
                                {
                                    if (iRst != 0)
                                    {
                                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                    }

                                    iRst = 9;
                                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                }
                                else if (outData.OUTDATA[0].RETVAL != 0)
                                {
                                    iRst = 1;
                                    hostMsg = $"EEER {ceid} {portState} Host NG({outData.OUTDATA[0].RETVAL}) Return";
                                }
                                else
                                {
                                    iRst = 0;
                                }
                                #endregion

                                #region 강제 배출 시 수동 포트 이동 설정
                                //$ 2022.11.08 : 대용량 방전기에서 강제 배출 시 Defect Limit Flag Y로 변경
                                // 2025-12-08 : 충방전기도 강제 배출 시  수동 포트 이동 설정 하도록 대방 조건 주석 처리
                                //if (this.EQPINFO__V_IS_POWER_BOX)
                                //{
                                if (portInfo.Port_State.ForcedOut == "Y")
                                {
                                    strBizName = "BR_SET_FORM_FORCE_MANUAL_PORT";
                                    CBR_SET_FORM_FORCE_MANUAL_PORT_IN inData_ForceOut = CBR_SET_FORM_FORCE_MANUAL_PORT_IN.GetNew(this);
                                    CBR_SET_FORM_FORCE_MANUAL_PORT_OUT outData_ForceOut = CBR_SET_FORM_FORCE_MANUAL_PORT_OUT.GetNew(this);
                                    inData_ForceOut.INDATA_LENGTH = 1;

                                    inData_ForceOut.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                                    inData_ForceOut.INDATA[0].IFMODE = IFMODE.ONLINE;
                                    inData_ForceOut.INDATA[0].USERID = USERID.EIF;

                                    inData_ForceOut.INDATA[0].EQPTID = portInfo.EqpID;
                                    inData_ForceOut.INDATA[0].CSTID = portInfo.Port_State.TrayIDs[0];
                                    inData_ForceOut.INDATA[0].FORM_FORCE_OUT = portInfo.Port_State.ForcedOut;

                                    iRst = BizCall(strBizName, portInfo.EqpID, inData_ForceOut, outData_ForceOut, out bizEx, txnID);
                                    if (iRst != 0 || outData_ForceOut == null)
                                    {
                                        if (iRst != 0)
                                        {
                                            _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData_ForceOut, bizEx);

                                            if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                        }

                                        iRst = 9;
                                        _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                    }
                                    else if (outData_ForceOut.OUTDATA[0].RETVAL != 0)
                                    {
                                        iRst = 1;
                                        hostMsg = $"EEER {ceid} {portState} Host NG({outData_ForceOut.OUTDATA[0].RETVAL}) Return(Forced Out)";
                                    }
                                    else
                                    {
                                        iRst = 0;
                                    }
                                }
                                //}
                                #endregion
                            }
                            else if (portState == ePortState.LC)
                            {
                                #region LC에서 ByPass 판단(Only. Inline Type 대용량 충방전Box)
                                if (this.EQPINFO__V_IS_POWER_BOX) //$ 2022.05.27 : 대용량 Box에 BCR이 있는 경우 BCR 값을 Host로 보고하여 예약 및 Bypass 여부를 판단(이전 Logic은 TrayID를 Empty로 보고 시 문제가 될 소지가 있어서 수정함)
                                {
                                    strBizName = "BR_SET_FORM_LOAD_END";
                                    CBR_SET_FORM_LOAD_END_IN inData = CBR_SET_FORM_LOAD_END_IN.GetNew(this);
                                    CBR_SET_FORM_LOAD_END_OUT outData = CBR_SET_FORM_LOAD_END_OUT.GetNew(this);

                                    //$ 2022.07.19 : BCR에서 읽은 값으로 EQPT_CUR를 설정하는 경우(RTD에서 예약을 해주면 안됨)
                                    inData.INDATA_LENGTH = 0;
                                    foreach (string trayID in portInfo.Port_State.TrayIDs)
                                    {
                                        inData.INDATA_LENGTH++;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                                        inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = portInfo.EqpID;
                                        inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = portInfo.Port_State.TrayIDs[inData.INDATA_LENGTH - 1];
                                    }

                                    iRst = BizCall(strBizName, portInfo.EqpID, inData, outData, out bizEx, txnID);
                                    if (iRst != 0 || outData == null)
                                    {
                                        if (iRst != 0)
                                        {
                                            _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                            if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                        }

                                        iRst = 9;
                                        _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                    }
                                    else if (outData.OUTDATA[0].RETVAL != 0)
                                    {
                                        if (outData.OUTDATA[0].RETVAL == 2) // ByPass인 경우 LC, UC 모두 보고 필요 없음(최종엽 책임님)
                                        {
                                            iRst = 2;
                                            hostMsg = $"EEER {ceid} {portState} Host ByPass({outData.OUTDATA[0].RETVAL}) Return";
                                        }
                                        else
                                        {
                                            iRst = 1;
                                            hostMsg = $"EEER {ceid} {portState} Host NG({outData.OUTDATA[0].RETVAL}) Return";
                                        }
                                    }
                                    else
                                    {
                                        iRst = 0;
                                    }

                                    //$ 2022.12.21 : 설비로 투입되는 Tray에 대해 TrayID를 MHS로 보고하여 반송 종료 처리 함(대용량의 경우 BCR이 있으니 LC 후)
                                    _EIFServer.MhsReport_LoadedCarrier(SIMULATION_MODE, this.HostObjID, portInfo.EqpID, portInfo.Port_State.TrayIDs);
                                }
                                #endregion
                            }

                            #region MHS로 Port 상태 보고
                            // iRst가 -1인 경우 초기 진입, iRst가 0인 경우 LC/UC에서 정상 처리 된 것, 이외에 값이면 LC/UC에서 문제 있었던거라 EQ로 Nak나 HostTrouble 주는 경우임
                            if (iRst == -1 || iRst == 0)
                            {
                                //$ 2022.07.21 : 일반 충방전기인 경우에만 MHS 보고 하게함.
                                if (this.EQPINFO__V_IS_POWER_BOX == false)
                                {
                                    string portID = _EIFServer.GetPortID(this.EQPINFO__V_PORT_TYPE, portInfo.EqpID, portState);  //만약 대용량 방전기도 해야 한다면 Virtual변수의 MachineID도 조합해야 함

                                    //$ 2021.10.19 : Y : Port 배출, N or Null : RTR일 수도 있고 공정 끝나서 Port 배출 일수 도 있음
                                    //$ 2022.05.10 : 온도보고에서 Dictionary가 만들어지기 전 배출 명령이 올 경우 해당 보고는 제외함(N으로 보내야 할 수도 있어 보이는데.. 테스트 시 외에는 없는 경우이니 제외)
                                    string trayOutValue = string.Empty;
                                    if (portState == ePortState.UR && this.ReqTrayOutFlag[ClientPacket.ObjectID])
                                        trayOutValue = this.ReqTrayOutFlag[ClientPacket.ObjectID] ? "Y" : "N";

                                    _EIFServer.MhsReport_PortStatus(SIMULATION_MODE, null, portInfo.EqpID, portID, portState, portInfo.Port_State.TrayIDs[0], eCarrierType.U, trayOutValue);
                                }

                                //$ 2021.10.19 : UR보고 후 Port 배출요청이 true이면 초기화 시킨다(UR 이후엔 무관)
                                if (portState == ePortState.UR && this.ReqTrayOutFlag[ClientPacket.ObjectID])
                                    this.ReqTrayOutFlag[ClientPacket.ObjectID] = false;

                                iRst = 0; //$ 2023.01.10 : MHS Port 상태 보고의 결과로 설비로 주는 보고가 NG가 되지는 않으니 0으로 고정
                            }
                            #endregion
                            #endregion
                        }
                        break;
                    case CEID._651:
                        {
                            #region CEID 651
                            eeer = JsonConvert.DeserializeObject<CBoxTemp_Info_EEER>(msgObject.ToString());
                            var eqInfo = (eeer as CBoxTemp_Info_EEER);

                            string eqpID = eqInfo.EqpID;

                            //$ 2025.07.08 : 전력량 및 유량계 사용 여부에 따라 JSON Parsing 여부 결정 (EIF DB Insert로 변경)
                            if (this.EQPINFO__V_IS_EUM_USE)
                            {
                                #region 전력/유량 Data Parsing
                                List<CEUMData> eumDataList = new List<CEUMData>();

                                #region 전력량 수집
                                if (eqInfo.Util_Info != null)
                                {
                                    for (int i = 0; i < eqInfo.Util_Info.Length; i++)
                                    {
                                        var item = eqInfo.Util_Info[i];
                                        var utilData = new CEUMData();

                                        utilData.EQPTID = eqpID;
                                        utilData.UTILTYPE = EUMTYPE.ELEC;

                                        utilData.METERNO = i + 1;
                                        utilData.R_CURRENT = item.R_CURRENT;
                                        utilData.T_CURRENT = item.T_CURRENT;
                                        utilData.S_CURRENT = item.S_CURRENT;
                                        utilData.RS_VOLTAGE = item.RS_VOLTAGE;
                                        utilData.TR_VOLTAGE = item.TR_VOLTAGE;
                                        utilData.ST_VOLTAGE = item.ST_VOLTAGE;
                                        utilData.ELECPOWER = item.ELECPOWER;
                                        utilData.WATTAGE = item.WATTAGE;
                                        utilData.POW_FACTOR = item.POW_FACTOR;

                                        eumDataList.Add(utilData);
                                    }
                                }
                                #endregion

                                #region 유량 수집
                                if (eqInfo.Flow_Info != null)
                                {
                                    for (int i = 0; i < eqInfo.Flow_Info.Length; i++)
                                    {
                                        var item = eqInfo.Flow_Info[i];
                                        var flowData = new CEUMData();

                                        flowData.EQPTID = eqpID;
                                        flowData.UTILTYPE = EUMTYPE.FLOW;
                                        flowData.METERNO = i + 1;
                                        flowData.INST_RATE = string.IsNullOrEmpty(item.Instant_Rate) ? "0" : item.Instant_Rate;
                                        flowData.INTG_RATE = string.IsNullOrEmpty(item.Integration_Rate) ? "0" : item.Integration_Rate;
                                        eumDataList.Add(flowData);
                                    }
                                }
                                #endregion

                                _EIFServer.EUMDataInsert(eumDataList, eqpID, this.HostObjectID);
                                #endregion
                            }

                            if (!this.Temp_Report_Cnt.ContainsKey(eqpID))
                            {
                                this.Temp_Report_Cnt.Add(eqpID, 0);
                                this.ReqTrayOutFlag.Add(eqpID, false); //$ 2021.10.19 : ECMD자체가 온도 보고시 Req되므로 여기서 Key 및 초기 값을 넣어둠                                
                            }
                            else
                                this.Temp_Report_Cnt[eqpID]--;

                            //$ 2018.12.02 : 해당 ObjectID의 값이 0 이하인 경우 온도데이터 DB 저장, 아닌 경우 Log만 찍음
                            if (this.Temp_Report_Cnt[eqpID] <= 0)
                            {
                                #region 온도 Data Parsing
                                this.Temp_Report_Cnt[eqpID] = this.EQPINFO__V_TEMP_COUNT;

                                /* bzActor Call */
                                strBizName = "BR_SET_FORM_REPORT_TEMP";
                                CBR_SET_FORM_REPORT_TEMP_IN inData = CBR_SET_FORM_REPORT_TEMP_IN.GetNew(this);
                                CBR_SET_FORM_REPORT_TEMP_OUT outData = CBR_SET_FORM_REPORT_TEMP_OUT.GetNew(this);
                                inData.INDATA_LENGTH = 1;

                                inData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                                inData.INDATA[0].IFMODE = IFMODE.ONLINE;
                                inData.INDATA[0].USERID = USERID.EIF;

                                inData.INDATA[0].EQPTID = eqpID;
                                inData.INDATA[0].STATUS = eqInfo.Temp_Info.EqpState;
                                inData.INDATA[0].MODE = eqInfo.Temp_Info.OperMode;
                                inData.INDATA[0].IS_TRAY = eqInfo.Temp_Info.TrayExist;

                                #region 온도 보고 데이터
                                //온도데이타
                                inData.TEMP_DATA_LENGTH = 1;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR1_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_01;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR2_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_02;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR3_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_03;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR4_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_04;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR5_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_05;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR6_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_06;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR7_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_07;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR8_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_08;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR9_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_09;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR10_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_10;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR11_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_11;
                                inData.TEMP_DATA[0].BTM_JIG_TMPR12_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_12;

                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR1_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_01;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR2_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_02;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR3_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_03;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR4_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_04;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR5_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_05;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR6_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_06;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR7_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_07;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR8_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_08;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR9_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_09;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR10_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_10;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR11_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_11;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR12_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_12;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR13_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_13;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR14_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_14;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR15_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_15;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR16_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_16;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR17_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_17;
                                inData.TEMP_DATA[0].BTM_PWSPLY_TMPR18_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_18;

                                inData.TEMP_DATA[0].BTM_JIG_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_JIG_AVG;
                                inData.TEMP_DATA[0].BTM_PWSPLY_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_LOW_POW_AVG;

                                inData.TEMP_DATA[0].TOP_JIG_TMPR1_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_01;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR2_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_02;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR3_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_03;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR4_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_04;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR5_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_05;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR6_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_06;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR7_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_07;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR8_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_08;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR9_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_09;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR10_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_10;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR11_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_11;
                                inData.TEMP_DATA[0].TOP_JIG_TMPR12_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_12;

                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR1_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_01;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR2_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_02;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR3_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_03;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR4_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_04;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR5_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_05;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR6_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_06;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR7_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_07;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR8_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_08;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR9_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_09;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR10_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_10;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR11_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_11;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR12_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_12;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR13_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_13;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR14_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_14;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR15_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_15;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR16_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_16;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR17_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_17;
                                inData.TEMP_DATA[0].TOP_PWSPLY_TMPR18_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_18;

                                inData.TEMP_DATA[0].TOP_JIG_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_JIG_AVG;
                                inData.TEMP_DATA[0].TOP_PWSPLY_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_UP_POW_AVG;

                                inData.TEMP_DATA[0].TOTL_JIG_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_JIG_AVG;
                                inData.TEMP_DATA[0].TOTL_PWSPLY_AVG_TMPR_VALUE = eqInfo.Temp_Info.TempDataArr.TEMP_POW_AVG;
                                #endregion

                                iRst = BizCall(strBizName, eqpID, inData, outData, out bizEx, txnID, this.BASE.TESTMODE__V_IS_TEMPLOG_USE);
                                if (iRst != 0 || outData == null)
                                {
                                    if (iRst != 0)
                                    {
                                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                    }

                                    iRst = 9;
                                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);
                                }
                                #endregion
                            }
                            else
                            {
                                iRst = 0;
                            }

                            EqpControlCmdSearch(ClientPacket);
                            #endregion
                        }
                        break;

                    default:
                        iRst = 9;
                        hostMsg = _EIFServer.ExceptionMessage("EEER Unknown CEID Error");
                        break;
                }

                EquipmentEventReportReply(ClientPacket, txnID, ceid, iRst.ToString(), hostMsg, replyInfo);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentEventReportReply(ClientPacket, txnID, ceid, new CEEER_R { EqpID = ClientPacket.ObjectID, CEID = ceid, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("EEER HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentEventReportReply(CPacket ClientPacket, string txnID, string ceid, string ret, string hostMsg, string replyInfo)
        {
            CEEER_R eeer_r = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                string dataSim = _EIFServer.GetJsonValue(typeof(CEEER_R));
                eeer_r = JsonConvert.DeserializeObject<CEEER_R>(dataSim);

                if (eeer_r == null) throw new Exception($"[{eeer_r.ToString()}] : Simulation Data Not Exist!!");

                // EqpID 경우 실제 Socket 연결된 데이터가 더 정확하여 이를 사용, ceid는 Simul에서 구분 불가해서 사용 필요, 그 외에 값은 Simul 데이터 사용, 필요시 주석 해제해서 인자로도 전송 가능
                eeer_r.EqpID = ClientPacket.ObjectID;
                eeer_r.CEID = ceid;
                //eeer_r.ACK = ret;
                //eeer_r.ReplyInfo = replyInfo; 
                #endregion
            }
            else
            {
                #region Real Case
                eeer_r = new CEEER_R { EqpID = ClientPacket.ObjectID, CEID = ceid, ACK = ret, HostMSG = hostMsg, ReplyInfo = replyInfo }; //$ 2021.02.01 : ReplyInfo 신규 추가
                #endregion
            }

            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(eeer_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, true, ceid); // 2025.05.22 :
        }

        protected virtual void EquipmentEventReportReply(CPacket ClientPacket, string txnID, string ceid, CEEER_R eeer_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(eeer_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false, ceid);
        }
        #endregion

        #region ERSR : Recipe 요청(ERSR - 101)
        protected virtual void EquipmentRecipeSpecRequest(CPacket ClientPacket)
        {
            int iRst = -1;
            string sRet = EXCEPTION_ERR_RET;
            String strBizName = string.Empty;
            string hostMsg = string.Empty;
            string troubleCd = string.Empty;

            string sGrpCD = string.Empty;
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                bool isTimeout = ReceiveMessageLogWrite(ClientPacket, txnID);
                if (isTimeout) return;

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];
                CERSR ersr = JsonConvert.DeserializeObject<CERSR>(msgObject.ToString());

                strBizName = "BR_GET_FORM_RECIPE";
                CBR_GET_FORM_RECIPE_IN inData = CBR_GET_FORM_RECIPE_IN.GetNew(this);
                CBR_GET_FORM_RECIPE_OUT outData = CBR_GET_FORM_RECIPE_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                if (ersr.TrayIDs != null)
                {
                    foreach (var trayID in ersr.TrayIDs)
                    {
                        inData.INDATA_LENGTH++;

                        inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                        inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                        inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                        inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = ClientPacket.ObjectID;

                        // JH 2024.09.25 대용량 충방전기의 경우 INDATA CSTID 사용한다고 하여 수정
                        if (this.EQPINFO__V_IS_POWER_BOX && !string.IsNullOrEmpty(trayID))
                            inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                    }
                }
                else
                {
                    inData.INDATA_LENGTH = 1;

                    inData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[0].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[0].USERID = USERID.EIF;

                    inData.INDATA[0].EQPTID = ClientPacket.ObjectID;
                }

                /* bzActor Call */
                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                if (iRst != 0 || outData == null)
                {
                    if (iRst != 0)
                    {
                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                    }

                    //$ 2021.12.22 : 해당 설비에 ERSR에서 Biz Error가 발생한 적이 있을 경우 Host Trouble 발생, 첫 발생인 경우 Log 찍고 Flag만 변경하여 20초 후 설비에서 다시 Recipe 요청하게 함
                    //               MCS 적용 후 Tray는 설비에 Load 되었으나, 정보상으로는 Update가 늦을 경우 Biz Error 발생에 대한 회피 Logic 반영
                    if (!this.IsBeforeBizErrOccur.ContainsKey(ClientPacket.ObjectID))
                        this.IsBeforeBizErrOccur.Add(ClientPacket.ObjectID, false);

                    if (this.IsBeforeBizErrOccur[ClientPacket.ObjectID] == true)
                    {
                        _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);

                        EquipmentRecipeSpecRequestReply(ClientPacket, txnID, sGrpCD, new CERSR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = hostMsg });
                        _EIFServer.SetLog("ERSR Host Error", this.HostObjID, ClientPacket.ObjectID);
                        this.IsBeforeBizErrOccur[ClientPacket.ObjectID] = false;
                    }
                    else
                    {
                        _EIFServer.SetLog("ERSR Host Error Occur!! Skip once", this.HostObjID, ClientPacket.ObjectID);
                        this.IsBeforeBizErrOccur[ClientPacket.ObjectID] = true;
                    }

                    return;
                }

                #region Recipe Data를 Structuer로 전환
                RecipeStruct rs = new RecipeStruct();
                sRet = outData.OUTDATA[0].RETVAL.ToString();
                sGrpCD = outData.OUTDATA[0].PROC_DETL_TYPE_CODE;

                rs.sProcid = outData.OUTDATA[0].PROCID;
                rs.sRoutid = outData.OUTDATA[0].ROUTID;
                rs.sLowerTrayID = outData.OUTDATA[0].LOWER_CSTID.Trim(); //$ 2020.08.14 : 1Tray인데 Space로 채워져서 Trim 침
                rs.sUpperTrayID = outData.OUTDATA[0].UPPER_CSTID.Trim(); //$ 2020.08.14 : 1Tray인데 Space로 채워져서 Trim 침
                rs.sNextProcess = outData.OUTDATA[0].NEXTPROCESS;
                rs.sChannelCnt = outData.OUTDATA[0].CHANNEL_COUNT;

                rs.sCellType = outData.OUTDATA[0].CELL_TYPE;
                rs.sTabDirection = outData.OUTDATA[0].TAB_DIRECTION;
                rs.iStepno = outData.OUTDATA[0].STEP_NO;
                rs.sLowerLotID = outData.OUTDATA[0].LOWER_LOTID;
                rs.sUpperLotID = outData.OUTDATA[0].UPPER_LOTID;
                rs.InitArray(9);

                // RECIPE_DATA
                for (int i = 0; i < rs.iStepno; i++)
                {
                    rs.dCccurnt[i] = outData.RECIPE_DATA[i].CCURNT_VALUE;
                    rs.dCvvoltage[i] = outData.RECIPE_DATA[i].CVLTG_VALUE;
                    rs.dEndtime[i] = outData.RECIPE_DATA[i].END_TIME;
                    rs.dJudgvoltage[i] = outData.RECIPE_DATA[i].JUDG_VLTG_VALUE;
                    rs.dEndcurrent[i] = outData.RECIPE_DATA[i].END_CURNT_VALUE;
                    rs.dEndcapacity[i] = outData.RECIPE_DATA[i].END_CAPA_VALUE;
                    rs.dEndvoltage[i] = outData.RECIPE_DATA[i].END_VLTG_VALUE;
                    rs.dWaittime[i] = outData.RECIPE_DATA[i].WAIT_TIME;
                    rs.dVoltvariation[i] = outData.RECIPE_DATA[i].PRFL_VLTG_DEVL_VALUE;
                    rs.dMgformcurnt[i] = outData.RECIPE_DATA[i].MGFORM_CURNT_VALUE;
                    rs.dMgformtime[i] = outData.RECIPE_DATA[i].MGFORM_TIME;
                    rs.dMgformmeasrtime[i] = outData.RECIPE_DATA[i].MGFORM_MEASR_TIME;
                }

                // LIMIT_DATA
                rs.dUpperlimitcurr = outData.LIMIT_DATA[0].ULMT_CURR_VALUE;
                rs.dLowerlimitcurr = outData.LIMIT_DATA[0].LLMT_CURR_VALUE;
                rs.dUpperlimitvolt = outData.LIMIT_DATA[0].ULMT_VOLT_VALUE;
                rs.dLowerlimitvolt = outData.LIMIT_DATA[0].LLMT_VOLT_VALUE;
                rs.dMegaUpperlimitcurr = outData.LIMIT_DATA[0].MEGA_ULMT_CURR_VALUE;
                rs.dMegaLowerlimitcurr = outData.LIMIT_DATA[0].MEGA_LLMT_CURR_VALUE;
                rs.dMegaUpperlimitvolt = outData.LIMIT_DATA[0].MEGA_ULMT_VOLT_VALUE;
                rs.dMegaLowerlimitvolt = outData.LIMIT_DATA[0].MEGA_LLMT_VOLT_VALUE;
                rs.dLimittime = outData.LIMIT_DATA[0].LIMIT_TIME;
                #endregion

                if (!sRet.Equals(OK_RET)) sRet = NG_RET;

                EquipmentRecipeSpecRequestReply(ClientPacket, txnID, sGrpCD, sRet, rs);

                //$ 2022.05.10 : Recipe 보고에 대한 처리가 완료되면, Biz Error Flag를 초기화 하여 새 Tray에서 에러나는 것도 한번은 막자
                if (this.IsBeforeBizErrOccur.ContainsKey(ClientPacket.ObjectID))
                    this.IsBeforeBizErrOccur[ClientPacket.ObjectID] = false;
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentRecipeSpecRequestReply(ClientPacket, txnID, sGrpCD, new CERSR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("ERSR HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentRecipeSpecRequestReply(CPacket ClientPacket, string txnID, string grpCd, string ret, RecipeStruct rData, string hostMsg = "")
        {
            CERSR_R ersr_r = null;
            JObject json = new JObject();

            if (SIMULATION_MODE)
            {
                #region Simul Case
                //$ 2024.01.04 : Simulation에서 작업할 공정은 사전에 선언된 배열에서 찾는다.
                eOperGroup opID = (this.EQPINFO__V_IS_POWER_BOX) ? this.m_ArrContPowWorks[this.ContiStep] : this.m_ArrContWorks[this.ContiStep];

                string dataSim = string.Empty;
                switch (opID)
                {
                    #region Charge
                    case eOperGroup.CHARGE:
                        dataSim = _EIFServer.GetJsonValue(typeof(CCHGERSR_R));
                        ersr_r = JsonConvert.DeserializeObject<CCHGERSR_R>(dataSim);
                        break;
                    #endregion

                    #region Discharge
                    case eOperGroup.DISCHARGE:
                        dataSim = _EIFServer.GetJsonValue(typeof(CDisCHGERSR_R));
                        ersr_r = JsonConvert.DeserializeObject<CDisCHGERSR_R>(dataSim);
                        break;
                    #endregion

                    #region OCV
                    case eOperGroup.OCV:
                        dataSim = _EIFServer.GetJsonValue(typeof(COCVERSR_R));
                        ersr_r = JsonConvert.DeserializeObject<COCVERSR_R>(dataSim);
                        break;
                    #endregion

                    #region Power Grading
                    case eOperGroup.POWERGRD:
                        dataSim = _EIFServer.GetJsonValue(typeof(CPOWGRDERSR_R));
                        ersr_r = JsonConvert.DeserializeObject<CPOWGRDERSR_R>(dataSim);
                        break;
                    #endregion

                    #region Power Charge
                    case eOperGroup.POWERCHG:
                        dataSim = _EIFServer.GetJsonValue(typeof(CPOWCHGERSR_R));
                        ersr_r = JsonConvert.DeserializeObject<CPOWCHGERSR_R>(dataSim);
                        break;
                    #endregion

                    default:
                        ersr_r.ACK = EXCEPTION_ERR_RET;
                        break;
                }

                if (ersr_r == null) throw new Exception($"[{ersr_r.ToString()}] : Simulation Data Not Exist!!");

                ersr_r.EqpID = ClientPacket.ObjectID;
                //ersr_r.ACK = "0";
                #endregion
            }
            else
            {
                #region Real Case
                eOperGroup opID = (eOperGroup)Enum.Parse(typeof(eOperGroup), grpCd, true);

                switch (opID)
                {
                    #region Charge
                    case eOperGroup.CHARGE:
                        {
                            ersr_r = new CCHGERSR_R();
                            var ChargeRecipe = (ersr_r as CCHGERSR_R);

                            ChargeRecipe.EqpID = ClientPacket.ObjectID;
                            ChargeRecipe.ACK = ret;
                            ChargeRecipe.HostMSG = hostMsg;
                            ChargeRecipe.TrayIDs = rData.TrayIDs;
                            ChargeRecipe.LotIDs = rData.LotIDs;
                            ChargeRecipe.OperGroupID = opID.ToString();
                            ChargeRecipe.RecipeID = rData.sProcid;
                            ChargeRecipe.NextProcess = rData.sNextProcess;
                            ChargeRecipe.ChannelType = rData.sChannelCnt;
                            ChargeRecipe.OperType = rData.sCellType;
                            ChargeRecipe.TabType = rData.sTabDirection;
                            ChargeRecipe.DegasType = AFTER_DEGAS;
                            ChargeRecipe.RecipeCount = Convert.ToString(rData.iStepno);

                            int recipeStepNum = Convert.ToInt32(rData.iStepno);

                            ChargeRecipe.ChgRecipe_Info = new CCHGRecipe[recipeStepNum];
                            ChargeRecipe.RecipeCount = $"{recipeStepNum}";

                            for (int i = 0; i < recipeStepNum; i++)
                            {
                                CCHGRecipe chgRecipe = new CCHGRecipe();
                                chgRecipe.CCCurrent = Convert.ToString(rData.dCccurnt[i]);
                                chgRecipe.CVVoltage = Convert.ToString(rData.dCvvoltage[i]);
                                chgRecipe.EndTime = Convert.ToString(rData.dEndtime[i]);
                                chgRecipe.JudgVoltage = Convert.ToString(rData.dJudgvoltage[i]);
                                chgRecipe.EndCurrent = Convert.ToString(rData.dEndcurrent[i]);
                                chgRecipe.EndCapacity = Convert.ToString(rData.dEndcapacity[i]);
                                chgRecipe.EndVoltage = Convert.ToString(rData.dEndvoltage[i]);
                                chgRecipe.WaitTime = Convert.ToString(rData.dWaittime[i]);
                                chgRecipe.VoltVariation = Convert.ToString(rData.dVoltvariation[i]);

                                ChargeRecipe.ChgRecipe_Info[i] = chgRecipe;
                            }

                            ChargeRecipe.Limit_Info = new CLimit();
                            ChargeRecipe.Limit_Info.UpperLimitCurr = Convert.ToString(rData.dUpperlimitcurr);
                            ChargeRecipe.Limit_Info.LowerLimitCurr = Convert.ToString(rData.dLowerlimitcurr);
                            ChargeRecipe.Limit_Info.UpperLimitVolt = Convert.ToString(rData.dUpperlimitvolt);
                            ChargeRecipe.Limit_Info.LowerLimitVolt = Convert.ToString(rData.dLowerlimitvolt);
                            ChargeRecipe.Limit_Info.LimitTime = Convert.ToString(rData.dLimittime);
                        }
                        break;
                    #endregion

                    #region Discharge
                    case eOperGroup.DISCHARGE:
                        {
                            ersr_r = new CDisCHGERSR_R();
                            var DisChargeRecipe = (ersr_r as CDisCHGERSR_R);

                            DisChargeRecipe.EqpID = ClientPacket.ObjectID;
                            DisChargeRecipe.ACK = ret;
                            DisChargeRecipe.HostMSG = hostMsg;
                            DisChargeRecipe.TrayIDs = rData.TrayIDs;
                            DisChargeRecipe.LotIDs = rData.LotIDs;
                            DisChargeRecipe.OperGroupID = opID.ToString();
                            DisChargeRecipe.RecipeID = rData.sProcid;
                            DisChargeRecipe.NextProcess = rData.sNextProcess;
                            DisChargeRecipe.ChannelType = rData.sChannelCnt;
                            DisChargeRecipe.OperType = rData.sCellType;
                            DisChargeRecipe.TabType = rData.sTabDirection;
                            DisChargeRecipe.DegasType = AFTER_DEGAS;
                            DisChargeRecipe.RecipeCount = Convert.ToString(rData.iStepno);

                            int recipeStepNum = Convert.ToInt32(rData.iStepno);

                            DisChargeRecipe.DischargeRecipe_Info = new CDisCHGRecipe[recipeStepNum];
                            DisChargeRecipe.RecipeCount = $"{recipeStepNum}";

                            for (int i = 0; i < recipeStepNum; i++)
                            {
                                CDisCHGRecipe disChgRecipe = new CDisCHGRecipe();
                                disChgRecipe.CCCurrent = Convert.ToString(rData.dCccurnt[i]);
                                disChgRecipe.EndTime = Convert.ToString(rData.dEndtime[i]);
                                disChgRecipe.EndVoltage = Convert.ToString(rData.dEndvoltage[i]);
                                disChgRecipe.EndCapacity = Convert.ToString(rData.dEndcapacity[i]);
                                disChgRecipe.WaitTime = Convert.ToString(rData.dWaittime[i]);

                                DisChargeRecipe.DischargeRecipe_Info[i] = disChgRecipe;
                            }

                            DisChargeRecipe.Limit_Info = new CLimit();
                            DisChargeRecipe.Limit_Info.UpperLimitCurr = Convert.ToString(rData.dUpperlimitcurr);
                            DisChargeRecipe.Limit_Info.LowerLimitCurr = Convert.ToString(rData.dLowerlimitcurr);
                            DisChargeRecipe.Limit_Info.UpperLimitVolt = Convert.ToString(rData.dUpperlimitvolt);
                            DisChargeRecipe.Limit_Info.LowerLimitVolt = Convert.ToString(rData.dLowerlimitvolt);
                            DisChargeRecipe.Limit_Info.LimitTime = Convert.ToString(rData.dLimittime);
                        }
                        break;
                    #endregion

                    #region OCV
                    case eOperGroup.OCV:
                        {
                            ersr_r = new COCVERSR_R();
                            var OCVRecipe = (ersr_r as COCVERSR_R);

                            OCVRecipe.EqpID = ClientPacket.ObjectID;
                            OCVRecipe.ACK = ret;
                            OCVRecipe.HostMSG = hostMsg;
                            OCVRecipe.TrayIDs = rData.TrayIDs;
                            OCVRecipe.LotIDs = rData.LotIDs;
                            OCVRecipe.OperGroupID = opID.ToString();
                            OCVRecipe.RecipeID = rData.sProcid;
                            OCVRecipe.NextProcess = rData.sNextProcess;
                            OCVRecipe.ChannelType = rData.sChannelCnt;
                            OCVRecipe.OperType = rData.sCellType;
                            OCVRecipe.TabType = rData.sTabDirection;
                            OCVRecipe.DegasType = AFTER_DEGAS;
                            OCVRecipe.RecipeCount = Convert.ToString(rData.iStepno);

                            int recipeStepNum = 1;
                            OCVRecipe.OCVRecipe_Info = new COCVRecipe[recipeStepNum]; //$ 2020.06.22 : OCV는 Step이 없으나 JIG등에서 쓰기 위해 구조는 유지
                            OCVRecipe.RecipeCount = $"{recipeStepNum}";

                            for (int i = 0; i < 1; i++)
                            {
                                COCVRecipe ocvRecipe = new COCVRecipe();
                                ocvRecipe.WaitTime = Convert.ToString(rData.dWaittime[i]);

                                OCVRecipe.OCVRecipe_Info[i] = ocvRecipe;
                            }

                            OCVRecipe.Limit_Info = new CLimit();
                            OCVRecipe.Limit_Info.UpperLimitCurr = Convert.ToString(rData.dUpperlimitcurr);
                            OCVRecipe.Limit_Info.LowerLimitCurr = Convert.ToString(rData.dLowerlimitcurr);
                            OCVRecipe.Limit_Info.UpperLimitVolt = Convert.ToString(rData.dUpperlimitvolt);
                            OCVRecipe.Limit_Info.LowerLimitVolt = Convert.ToString(rData.dLowerlimitvolt);
                            OCVRecipe.Limit_Info.LimitTime = Convert.ToString(rData.dLimittime);
                        }
                        break;
                    #endregion                    

                    #region Power Grading
                    case eOperGroup.POWERGRD:
                        {
                            ersr_r = new CPOWGRDERSR_R();
                            var PowGradingRecipe = (ersr_r as CPOWGRDERSR_R);

                            PowGradingRecipe.EqpID = ClientPacket.ObjectID;
                            PowGradingRecipe.ACK = ret;
                            PowGradingRecipe.HostMSG = hostMsg;
                            PowGradingRecipe.TrayIDs = rData.TrayIDs;
                            PowGradingRecipe.LotIDs = rData.LotIDs;
                            PowGradingRecipe.OperGroupID = opID.ToString();
                            PowGradingRecipe.RecipeID = rData.sProcid;
                            PowGradingRecipe.NextProcess = rData.sNextProcess;
                            PowGradingRecipe.ChannelType = rData.sChannelCnt;
                            PowGradingRecipe.OperType = rData.sCellType;
                            PowGradingRecipe.TabType = rData.sTabDirection;
                            PowGradingRecipe.DegasType = AFTER_DEGAS;

                            int recipeStepNum = 1;
                            PowGradingRecipe.PowGrdRecipe_Info = new CPOWGRDRecipe[recipeStepNum]; //$ 2020.07.13 : Power Grading은 Step이 없으나 JIG등에서 쓰기 위해 구조는 유지
                            PowGradingRecipe.RecipeCount = $"{recipeStepNum}";

                            for (int i = 0; i < 1; i++)
                            {
                                CPOWGRDRecipe powerGradingRecipe = new CPOWGRDRecipe();
                                powerGradingRecipe.DisChgCurrent = Convert.ToString(rData.dMgformcurnt[i]);
                                powerGradingRecipe.DischargeTime = Convert.ToString(rData.dMgformtime[i]);
                                powerGradingRecipe.MeasureTime = Convert.ToString(rData.dMgformmeasrtime[i]);

                                PowGradingRecipe.PowGrdRecipe_Info[i] = powerGradingRecipe;
                            }

                            PowGradingRecipe.Limit_Info = new CLimit();
                            PowGradingRecipe.Limit_Info.UpperLimitCurr = Convert.ToString(rData.dMegaUpperlimitcurr);
                            PowGradingRecipe.Limit_Info.LowerLimitCurr = Convert.ToString(rData.dMegaLowerlimitcurr);
                            PowGradingRecipe.Limit_Info.UpperLimitVolt = Convert.ToString(rData.dMegaUpperlimitvolt);
                            PowGradingRecipe.Limit_Info.LowerLimitVolt = Convert.ToString(rData.dMegaLowerlimitvolt);
                            PowGradingRecipe.Limit_Info.LimitTime = Convert.ToString(rData.dLimittime);
                        }
                        break;
                    #endregion

                    #region Power Charge
                    case eOperGroup.POWERCHG:
                        {
                            ersr_r = new CPOWCHGERSR_R();
                            var PowCharginRecipe = (ersr_r as CPOWCHGERSR_R);

                            PowCharginRecipe.EqpID = ClientPacket.ObjectID;
                            PowCharginRecipe.ACK = ret;
                            PowCharginRecipe.HostMSG = hostMsg;
                            PowCharginRecipe.TrayIDs = rData.TrayIDs;
                            PowCharginRecipe.LotIDs = rData.LotIDs;
                            PowCharginRecipe.OperGroupID = opID.ToString();
                            PowCharginRecipe.RecipeID = rData.sProcid;
                            PowCharginRecipe.NextProcess = rData.sNextProcess;
                            PowCharginRecipe.ChannelType = rData.sChannelCnt;
                            PowCharginRecipe.OperType = rData.sCellType;
                            PowCharginRecipe.TabType = rData.sTabDirection;
                            PowCharginRecipe.DegasType = AFTER_DEGAS;

                            int recipeStepNum = 1;
                            PowCharginRecipe.PowChgRecipe_Info = new CPOWCHGRecipe[recipeStepNum]; //$ 2020.07.13 : Power Chaging은 Step이 없으나 JIG등에서 쓰기 위해 구조는 유지
                            PowCharginRecipe.RecipeCount = $"{recipeStepNum}";

                            for (int i = 0; i < 1; i++)
                            {
                                CPOWCHGRecipe powerChagingRecipe = new CPOWCHGRecipe();
                                powerChagingRecipe.ChgCurrent = Convert.ToString(rData.dMgformcurnt[i]);
                                powerChagingRecipe.ChargeTime = Convert.ToString(rData.dMgformtime[i]);
                                powerChagingRecipe.MeasureTime = Convert.ToString(rData.dMgformmeasrtime[i]);

                                PowCharginRecipe.PowChgRecipe_Info[i] = powerChagingRecipe;
                            }

                            PowCharginRecipe.Limit_Info = new CLimit();
                            PowCharginRecipe.Limit_Info.UpperLimitCurr = Convert.ToString(rData.dMegaUpperlimitcurr);
                            PowCharginRecipe.Limit_Info.LowerLimitCurr = Convert.ToString(rData.dMegaLowerlimitcurr);
                            PowCharginRecipe.Limit_Info.UpperLimitVolt = Convert.ToString(rData.dMegaUpperlimitvolt);
                            PowCharginRecipe.Limit_Info.LowerLimitVolt = Convert.ToString(rData.dMegaLowerlimitvolt);
                            PowCharginRecipe.Limit_Info.LimitTime = Convert.ToString(rData.dLimittime);
                        }
                        break;
                    #endregion

                    default:
                        ersr_r.EqpID = ClientPacket.ObjectID;
                        ersr_r.ACK = EXCEPTION_ERR_RET;
                        ersr_r.HostMSG = _EIFServer.ExceptionMessage($"ERSR {opID} Not Exist HOST Exception ERROR");
                        break;
                }
                #endregion
            }

            json.Add(ClientPacket.SendMsgID, JObject.FromObject(ersr_r));
            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, true, grpCd);


            //$ 2022.12.21 : 설비로 투입되는 Tray에 대해 TrayID를 MHS로 보고하여 반송 종료 처리 함(충방전기의 경우 Recipe 요청 완료 후)
            if (!this.EQPINFO__V_IS_POWER_BOX)
                _EIFServer.MhsReport_LoadedCarrier(SIMULATION_MODE, this.HostObjID, ersr_r.EqpID, ersr_r.TrayIDs);
        }

        protected virtual void EquipmentRecipeSpecRequestReply(CPacket ClientPacket, string txnID, string grpCd, CERSR_R ersr_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(ersr_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false, grpCd);
        }
        #endregion

        #region ELIR : Channel 작업 여부 요청(ELIR - 140)
        protected virtual void EquipmentLotInformationDataRequest(CPacket ClientPacket)
        {
            int iRst = -1;
            Exception bizEx;
            string hostMsg = string.Empty;
            string troubleCd = string.Empty;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                bool isTimeout = ReceiveMessageLogWrite(ClientPacket, txnID);
                if (isTimeout) return;

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];
                CELIR elir = JsonConvert.DeserializeObject<CELIR>(msgObject.ToString());

                string sOperGroupID = elir.OPERGROUPID;
                string sLowerTrayID = elir.TrayIDs[0];
                string sUpperTrayID = (elir.TrayIDs.Length >= 2) ? elir.TrayIDs[1] : string.Empty; //$ 2020.07.07 : 임시로 처리 함

                /* bzActor Call */
                string strBizName = "BR_GET_FORM_CELL_WORKABLE";
                CBR_GET_FORM_CELL_WORKABLE_IN inData = CBR_GET_FORM_CELL_WORKABLE_IN.GetNew(this);
                CBR_GET_FORM_CELL_WORKABLE_OUT outData = CBR_GET_FORM_CELL_WORKABLE_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                foreach (var trayID in elir.TrayIDs)
                {
                    if (String.IsNullOrWhiteSpace(trayID)) continue;  //$ 2020.06.22 : Lower TrayID는 무조건 있다고 보는건가..

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayID;
                }

                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                if (iRst != 0 || outData == null)
                {
                    if (iRst != 0)
                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                    _EIFServer.BizException(SIMULATION_MODE, strBizName, ClientPacket.ObjectID, bizEx, out troubleCd, out hostMsg);

                    EquipmentLotInformationDataRequestReply(ClientPacket, txnID, new CELIR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = hostMsg });
                    _EIFServer.SetLog("ELIR Host Error", this.HostObjID, ClientPacket.ObjectID);
                    return;
                }

                LotInfoStruct li = new LotInfoStruct();
                li.sLowerTrayID = sLowerTrayID;
                li.sUpperTrayID = sUpperTrayID;
                li.sLowerLotID = outData.OUTDATA[0].BTM_LOTID;
                li.sUpperLotID = outData.OUTDATA[0].TOP_LOTID;
                li.sLowerChannelOrder = outData.OUTDATA[0].BTM_WORKABLE_SUBLOT;
                li.sUpperChannelOrder = outData.OUTDATA[0].TOP_WORKABLE_SUBLOT;
                li.sCellBadLimitCnt = outData.OUTDATA[0].SUBLOT_DFCT_LIMIT_QTY;
                li.sCellBadLimitUseYn = outData.OUTDATA[0].SUBLOT_DFCT_LIMIT_USE_FLAG;

                //$ 2023.03.15 : MAVD 적용에 대한 Biz Mapping 부분
                if (!this.EQPINFO__V_IS_POWER_BOX)
                {
                    int lowCellCnt = li.sLowerChannelOrder.Length;
                    int upCellCnt = li.sUpperChannelOrder.Trim().Length; // HDH 2023.03.15 : Tray 1개일 경우 Space 채워져서 Trim 사용

                    li.CellCnts = new string[] { lowCellCnt.ToString(), upCellCnt.ToString() };
                    li.PkgLotIDs = new string[] { outData.OUTDATA[0].BTM_DAY_GR_LOTID, upCellCnt == 0 ? string.Empty : outData.OUTDATA[0].TOP_DAY_GR_LOTID };

                    li.LoCellIDLists = outData.OUTDATA[0].BTM_SUBLOTID.Split(',');
                    li.UpCellIDLists = upCellCnt == 0 ? new string[] { } : outData.OUTDATA[0].TOP_SUBLOTID.Split(',');
                }

                iRst = 0;
                if (outData.OUTDATA[0].RETVAL != 0)
                {
                    iRst = 1;
                    _EIFServer.SetLog($"ELIR Host Nak({iRst})", this.HostObjID, ClientPacket.ObjectID);
                    hostMsg = $"ELIR Host Nak({iRst})";
                }

                EquipmentLotInformationDataRequestReply(ClientPacket, txnID, iRst.ToString(), li, hostMsg);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentLotInformationDataRequestReply(ClientPacket, txnID, new CELIR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("ELIR HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentLotInformationDataRequestReply(CPacket ClientPacket, string txnID, string ret, LotInfoStruct liData, string hostMsg = "")
        {
            CELIR_R elir_r = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                if (this.EQPINFO__V_IS_POWER_BOX)
                {
                    string dataSim = _EIFServer.GetJsonValue(typeof(CBoxELIR_R));
                    elir_r = JsonConvert.DeserializeObject<CBoxELIR_R>(dataSim);
                }
                else
                {
                    string dataSim = _EIFServer.GetJsonValue(typeof(CCDCELIR_R));
                    elir_r = JsonConvert.DeserializeObject<CCDCELIR_R>(dataSim);
                }

                if (elir_r == null) throw new Exception($"[{elir_r.ToString()}] : Simulation Data Not Exist!!");

                elir_r.EqpID = ClientPacket.ObjectID; // EqpID 경우 실제 Socket 연결된 데이터가 더 정확하여 이를 사용, 그 외에 값은 Simul 데이터 사용, 필요시 주석 해제해서 인자로도 전송 가능
                //elir_r.ACK = ret;
                //elir_r.TrayIDs = liData.TrayIDs;
                //elir_r.LotIDs = liData.LotIDs;
                #endregion
            }
            else
            {
                #region Real Case
                if (this.EQPINFO__V_IS_POWER_BOX)
                {
                    elir_r = new CBoxELIR_R  //ACK나 NAK인 경우 충방전Box 구조를 유지
                    {
                        EqpID = ClientPacket.ObjectID,
                        ACK = ret,
                        HostMSG = hostMsg,
                        TrayIDs = liData.TrayIDs,
                        LotIDs = liData.LotIDs,
                        CWP = liData.CWPs,
                        MarginOfErr = liData.sCellBadLimitCnt,
                        UseMargin = liData.sCellBadLimitUseYn,
                    };
                }
                else
                {
                    elir_r = new CCDCELIR_R
                    {
                        EqpID = ClientPacket.ObjectID,
                        ACK = ret,
                        HostMSG = hostMsg,
                        TrayIDs = liData.TrayIDs,
                        LotIDs = liData.LotIDs,
                        CWP = liData.CWPs,
                        MarginOfErr = liData.sCellBadLimitCnt,
                        UseMargin = liData.sCellBadLimitUseYn,
                        CellCnts = liData.CellCnts,
                        PKGLotIDs = liData.PkgLotIDs,
                        LowerCellIDList = liData.LoCellIDLists,
                        UpperCellIDList = liData.UpCellIDLists
                    };
                }
                #endregion
            }

            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(elir_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID);
        }

        protected virtual void EquipmentLotInformationDataRequestReply(CPacket ClientPacket, string txnID, CELIR_R elir_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(elir_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false);
        }
        #endregion


        #region ECMD_R : Equipment Command Receive(ECMD_R - 130, 134, 138)
        private void EquipmentCommandRecv(CPacket ClientPacket)
        {
            int iRst = -1;
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string sCode = ClientPacket.Body.ToString();
                string sCtrStatus = string.Empty;

                ReceiveMessageLogWrite(ClientPacket, txnID, true); //$ 2023.01.05 : 테스트 무관하므로 true

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];

                CECMD_R ecmd_r = JsonConvert.DeserializeObject<CECMD_R>(msgObject.ToString());

                string strBizName = "BR_GET_EQP_STATUS";
                string strSetBizName = "BR_SET_EQP_CTR_STATUS";

                switch (ecmd_r.RCMD)
                {
                    case "U":
                    case "L": //$ 2025.07.16 : Box To Box 상태 처리 추가
                        #region Unload [138]
                        /// <summary> 설비 Tray Unload 지시 응답 </summary>
                        /// <br>설비제어상태 : Tray Unload  >>  'U' ->  제어 성공 시 'N'</br>
                        /// <br>설비제어상태 : Tray RtoR    >>  'U' ->  제어 성공 시 'K'</br>
                        /// <br>설비제어상태 : 제어 실패    >>  null (설비에서 거부 시 재 명령 내려가지 않도록 null 처리)</br>
                        if (ecmd_r.ACK.Equals(OK_RET))   //정상 정지
                        {
                            /* bzActor Call */
                            CBR_GET_EQP_STATUS_IN inData_GES = CBR_GET_EQP_STATUS_IN.GetNew(this);
                            CBR_GET_EQP_STATUS_OUT outData_GES = CBR_GET_EQP_STATUS_OUT.GetNew(this);
                            inData_GES.IN_EQP_LENGTH = 1;

                            inData_GES.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_GES.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_GES.IN_EQP[0].USERID = USERID.EIF;

                            inData_GES.IN_EQP[0].EQPTID = ClientPacket.ObjectID;

                            iRst = BizCall(strBizName, ClientPacket.ObjectID, inData_GES, outData_GES, out bizEx, txnID);
                            if (iRst != 0 || outData_GES == null)
                            {
                                if (iRst != 0)
                                {
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData_GES, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                }

                                return;
                            }

                            sCode = outData_GES.OUTDATA[0].EQPT_CTRL_STAT_CODE;

                            if (sCode == OPER_TRAY_UNLOAD)
                            {
                                sCtrStatus = "N";
                                this.ReqTrayOutFlag[ClientPacket.ObjectID] = true; //$ 2021.10.19 : UI에서 Port 배출 명령했고, 설비에서 Tray 배출이 가능하다고 했으므로 True로 변경
                            }
                            else
                                sCtrStatus = "K";

                            /* bzActor Call */
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        else   //정지 실패
                        {
                            /* bzActor Call */
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;    //설비에서 거부 시 재 명령 내려가지 않도록 null 처리

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        #endregion
                        break;

                    case "P":
                    case "E":
                        #region Pause, End [130]
                        /// <summary> 설비작업 정지 지시 응답 </summary>
                        /// <br>설비제어상태 : 일시정지     >>  'P' ->  제어 성공 시 'T'</br>
                        /// <br>설비제어상태 : 현재공정종료 >>  'E' ->  제어 성공 시 'V'</br>
                        /// <br>설비제어상태 : 제어 실패    >>  null (설비에서 거부 시 재 명령 내려가지 않도록 null 처리)</br>
                        if (ecmd_r.ACK.Equals(OK_RET))   //정상 정지
                        {
                            /* bzActor Call */
                            CBR_GET_EQP_STATUS_IN inData_GES = CBR_GET_EQP_STATUS_IN.GetNew(this);
                            CBR_GET_EQP_STATUS_OUT outData_GES = CBR_GET_EQP_STATUS_OUT.GetNew(this);
                            inData_GES.IN_EQP_LENGTH = 1;

                            inData_GES.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_GES.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_GES.IN_EQP[0].USERID = USERID.EIF;

                            inData_GES.IN_EQP[0].EQPTID = ClientPacket.ObjectID;

                            iRst = BizCall(strBizName, ClientPacket.ObjectID, inData_GES, outData_GES, out bizEx, txnID);
                            if (iRst != 0 || outData_GES == null)
                            {
                                if (iRst != 0)
                                {
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData_GES, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                }

                                return;
                            }

                            sCode = outData_GES.OUTDATA[0].EQPT_CTRL_STAT_CODE;

                            if (sCode == OPER_PAUSE)
                                sCtrStatus = "T";
                            else
                                sCtrStatus = "V";

                            /* bzActor Call */
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        else   //정지 실패
                        {
                            /* bzActor Call */
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        #endregion
                        break;

                    case "R":
                    case "S":
                    case "I":
                        #region Resume, reStart [134]
                        /// <summary>설비작업 재시작 지시 응답 </summary>
                        /// <br>설비제어상태 : 연속시작     >>  'S' ->  제어 성공 시 'W'</br>
                        /// <br>설비제어상태 : 재시작       >>  'Q' ->  제어 성공 시 'U'</br>
                        /// <br>설비제어상태 : 설비초기화   >>  'I' ->  제어 성공 시 'J'</br>
                        /// <br>설비제어상태 : 제어 실패    >>  null (설비에서 거부 시 재 명령 내려가지 않도록 null 처리)</br>
                        if (ecmd_r.ACK.Equals(OK_RET))
                        {
                            /* bzActor Call */
                            CBR_GET_EQP_STATUS_IN inData_GES = CBR_GET_EQP_STATUS_IN.GetNew(this);
                            CBR_GET_EQP_STATUS_OUT outData_GES = CBR_GET_EQP_STATUS_OUT.GetNew(this);
                            inData_GES.IN_EQP_LENGTH = 1;

                            inData_GES.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_GES.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_GES.IN_EQP[0].USERID = USERID.EIF;

                            inData_GES.IN_EQP[0].EQPTID = ClientPacket.ObjectID;

                            iRst = BizCall(strBizName, ClientPacket.ObjectID, inData_GES, outData_GES, out bizEx, txnID);
                            if (iRst != 0 || outData_GES == null)
                            {
                                if (iRst != 0)
                                {
                                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData_GES, bizEx);

                                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                                }

                                return;
                            }

                            sCode = outData_GES.OUTDATA[0].EQPT_CTRL_STAT_CODE;

                            if (sCode == OPER_RESUME)
                                sCtrStatus = "W";
                            else if (sCode == OPER_RESTART)
                                sCtrStatus = "U";
                            else
                                sCtrStatus = "J";

                            /* bzActor Call */
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        else   //설비제어 실패
                        {
                            CBR_SET_EQP_CTR_STATUS_IN inData_ECS = CBR_SET_EQP_CTR_STATUS_IN.GetNew(this);
                            CBR_SET_EQP_CTR_STATUS_OUT outData_ECS = CBR_SET_EQP_CTR_STATUS_OUT.GetNew(this);
                            inData_ECS.IN_EQP_LENGTH = 1;

                            inData_ECS.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                            inData_ECS.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                            inData_ECS.IN_EQP[0].USERID = USERID.EIF;

                            inData_ECS.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                            inData_ECS.IN_EQP[0].EQPT_CTRL_STAT_CODE = sCtrStatus;

                            iRst = BizCall(strSetBizName, ClientPacket.ObjectID, inData_ECS, outData_ECS, out bizEx, txnID);
                            if (iRst != 0)
                            {
                                _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strSetBizName, string.Empty, inData_ECS, bizEx);

                                if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                            }
                        }
                        #endregion
                        break;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }
        #endregion

        #region EALM : 설비Trouble 보고(EALM - 985)
        private void EquipmentAlarmReport(CPacket ClientPacket)
        {
            int iRst = -1;
            Exception bizEx;
            string strBizName = string.Empty;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                ReceiveMessageLogWrite(ClientPacket, txnID, true); //$ 2023.01.05 : 테스트 무관하므로 true

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];

                CEALM ealm = JsonConvert.DeserializeObject<CEALM>(msgObject.ToString());  //$ 2020.06.15 : Alarm에 대한 Set, Reset을 어찌 해야 하나?? 기존엔 없었어..

                strBizName = "BR_SET_EQP_STATUS";
                /* bzActor Call */
                CBR_SET_EQP_STATUS_IN inData = CBR_SET_EQP_STATUS_IN.GetNew(this);
                CBR_SET_EQP_STATUS_OUT outData = CBR_SET_EQP_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = ClientPacket.ObjectID;
                inData.IN_EQP[0].EIOSTAT = "T";

                //$ 2022.07.21 : Trouble Code 5 -> 6자리 고정
                string alarmID = ealm.AlarmID;
                int iAlarmID = 0;
                if (!int.TryParse(alarmID, out iAlarmID)) // 숫자형이 아닌 경우 1만으로 고정, 숫자형은 변환 가능시 6자리 문자열로 변경
                {
                    iAlarmID = EIFALMCD.EQDEFLT;
                    _EIFServer.SetWarnLog($"AlarmID ({alarmID}) Not Integer!!", this.HostObjID, ClientPacket.ObjectID);
                }

                inData.IN_EQP[0].ALARM_ID = iAlarmID.ToString("D6");

                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }
        #endregion

        #region EUMR : Unknown Command Send(EUMR - 981) : 1- Unknown Cmd, 2- Size Error, 3- Checksum Error        
        private void EquipmentUnknownMessageReport(CPacket ClientPacket) // Recv EUMR by Eq
        {
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                ReceiveMessageLogWrite(ClientPacket, txnID, true); //$ 2023.01.05 : 테스트 무관하므로 true
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }

        private void HostUnknownMessageReport(CSocketClient client, string sCode, string sPacket) // Send EUMR by Host
        {
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string msgID = "EUMR";

                CEUMR eumr = new CEUMR { EqpID = this.HostObjID, ACK = sCode };

                JObject json = new JObject();
                json.Add(msgID, JObject.FromObject(eumr));

                SendHostMsg(client, txnID, msgID, NO_REPLY, json.ToString());
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, this.HostObjID);
            }
        }
        #endregion

        #region EDNT : 시간 동기화(EDNT)
        private void EquipmentDateAndTimeSync(CPacket ClientPacket)
        {
            string txnID = _EIFServer.GenerateTransactionKey();

            ReceiveMessageLogWrite(ClientPacket, "", true); //$ 2023.01.05 : 테스트 무관하므로 true
        }
        #endregion

        #region ESDR : 연기 감지 보고(ESDR) //$ 2024.03.19 : ESHD 부터 추가되는 화재 관련 보고를 위한 PC Type 신규 Message Set
        protected virtual void EquipmentSmokeDectectReport(CPacket ClientPacket)
        {
            int iRst = -1;
            string sRet = EXCEPTION_ERR_RET;
            String strBizName = string.Empty;
            string hostMsg = string.Empty;

            string sGrpCD = string.Empty;
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                //2024.03.15 PC 사전검수모드 NAK+Timeout 적용 우선 EEER 제외 메세지 (STEP 존재하는 메세지 포함) 1번만 실행될 수 있게 함.  
                bool isTimeout = ReceiveMessageLogWrite(ClientPacket, "");
                if (isTimeout) return;

                JObject jo = JObject.Parse(ClientPacket.Body);
                JToken msgObject = jo[ClientPacket.RecvMgsID];
                CESDR esdr = JsonConvert.DeserializeObject<CESDR>(msgObject.ToString());

                strBizName = "BR_SET_FORM_FIRE_OCCUR_NEW";
                CBR_SET_FORM_FIRE_OCCUR_NEW_IN inData = CBR_SET_FORM_FIRE_OCCUR_NEW_IN.GetNew(this);
                CBR_SET_FORM_FIRE_OCCUR_NEW_OUT outData = CBR_SET_FORM_FIRE_OCCUR_NEW_OUT.GetNew(this);
                inData.INDATA_LENGTH = 1;

                inData.INDATA[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.INDATA[0].IFMODE = IFMODE.ONLINE;
                inData.INDATA[0].USERID = USERID.EIF;

                inData.INDATA[0].EQPTID = ClientPacket.ObjectID;
                inData.INDATA[0].SMOKE_DETECT = esdr.SmokeStatus.Trim();
                inData.INDATA[0].TRAY_EXIST = esdr.TrayExist.Trim();

                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                    if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                }

                if (outData.OUTDATA_LENGTH > 0)
                    sRet = outData.OUTDATA[0].RETVAL.ToString();

                if (!sRet.Equals(OK_RET)) sRet = NG_RET;

                EquipmentSmokeDectectReportReply(ClientPacket, txnID, sRet, hostMsg);

                SmokeReport(ClientPacket.ObjectID, esdr.SmokeStatus.Trim());  //$ 2025.07.16 : 관제 시스템으로 보고용 Method 호출
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
                EquipmentSmokeDectectReportReply(ClientPacket, txnID, new CESDR_R { EqpID = ClientPacket.ObjectID, ACK = EXCEPTION_ERR_RET, HostMSG = _EIFServer.ExceptionMessage("ESDR HOST EXCEPTION ERROR") });
            }
        }

        protected virtual void EquipmentSmokeDectectReportReply(CPacket ClientPacket, string txnID, string ret, string hostMsg)
        {
            CESDR_R esdr_r = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                string dataSim = _EIFServer.GetJsonValue(typeof(CESDR_R));
                esdr_r = JsonConvert.DeserializeObject<CESDR_R>(dataSim);

                if (esdr_r == null) throw new Exception($"[{esdr_r.ToString()}] : Simulation Data Not Exist!!");

                esdr_r.EqpID = ClientPacket.ObjectID; // EqpID 경우 실제 Socket 연결된 데이터가 더 정확하여 이를 사용, 그 외에 값은 Simul 데이터 사용, 필요시 주석 해제해서 인자로도 전송 가능
                //eapd_r.ACK = ret;         
                //eapd_r.HostMSG = hostMsg;
                #endregion
            }
            else
            {
                #region Real Case
                esdr_r = new CESDR_R { EqpID = ClientPacket.ObjectID, ACK = ret, HostMSG = hostMsg };
                #endregion
            }

            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(esdr_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID);
        }

        protected virtual void EquipmentSmokeDectectReportReply(CPacket ClientPacket, string txnID, CESDR_R esdr_r)
        {
            JObject json = new JObject();
            json.Add(ClientPacket.SendMsgID, JObject.FromObject(esdr_r));

            ClientPacket.SendBodyList.Add("1", json);
            SendReplyMsg(ClientPacket, txnID, false);
        }
        #endregion
        #endregion

        #region EERR Thread Method
        private int PreInterval = 0;
        private void TimerSearch()
        {
            try
            {
                //Wait(this.BASE.EQPINFO__V_WAITTIME); //$ 2025.10.31 : Wait를 Scheduler 함수 내부에서 호출 시 해당 시간 만큼 무중단 패치 시 Delay걸려 주석 처리

                EquipmentEventReportRequest();

                EIFMonitoringData();

                //$ 2025.10.31 : Wait를 통해 Timer 조절이 불가능하므로 Scheduler Interval을 변경하는 방식으로 전환
                if (this.PreInterval == 0 || this.PreInterval != this.BASE.EQPINFO__V_WAITTIME)
                {
                    this.SchedulerHandlers["TimerSearch"].Interval = this.BASE.EQPINFO__V_WAITTIME;
                    this.PreInterval = this.BASE.EQPINFO__V_WAITTIME;
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, this.HostObjID);
            }
        }

        //$ 2020.06.08 : 909를 대신해서 EERR로 요청(활성화의 경우 909 온도 보고 요청 이외에는 쓸 일이..)
        protected virtual void EquipmentEventReportRequest()
        {
            try
            {
                foreach (KeyValuePair<string, CSocketClient> temp in this.BASE.SocketClientList)
                {
                    string msgID = "EERR";

                    CEERR eerr = new CEERR { EqpID = this.HostObjID, CEID = CEID._651 };

                    JObject json = new JObject();
                    json.Add(msgID, JObject.FromObject(eerr));

                    SendHostMsg((CSocketClient)temp.Value, "", msgID, EX_REPLY, json.ToString());
                    Wait(this.EQPINFO__V_INTERVAL); //$ 2018.12.02 : 설비별로 909 요청시간에 Term을 줘서 910 메시지가 몰리지 않게 한다.
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, this.HostObjID);
            }
        }
        #endregion

        #region ECMD : 설비작업제어지시
        protected virtual void EqpControlCmdSearch(CPacket ClientPacket)
        {
            int iRst = -1;
            String strBizName = "BR_GET_EQP_STATUS";
            Exception bizEx;

            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                /* bzActor Call */
                CBR_GET_EQP_STATUS_IN inData = CBR_GET_EQP_STATUS_IN.GetNew(this);
                CBR_GET_EQP_STATUS_OUT outData = CBR_GET_EQP_STATUS_OUT.GetNew(this);
                inData.IN_EQP_LENGTH = 1;

                inData.IN_EQP[0].SRCTYPE = SRCTYPE.EQUIPMENT;
                inData.IN_EQP[0].IFMODE = IFMODE.ONLINE;
                inData.IN_EQP[0].USERID = USERID.EIF;

                inData.IN_EQP[0].EQPTID = ClientPacket.ObjectID;

                iRst = BizCall(strBizName, ClientPacket.ObjectID, inData, outData, out bizEx, txnID);
                if (iRst != 0 || outData == null)
                {
                    if (iRst != 0)
                    {
                        _EIFServer.RegBizRuleException(SIMULATION_MODE, ClientPacket.ObjectID, strBizName, string.Empty, inData, bizEx);

                        if (bizEx != null) _EIFServer.SetSolExcepLog(ClientPacket.ObjectID, txnID, bizEx.ToString()); // 2025.05.22 : Biz Exception Solace 전송
                    }

                    return;
                }

                string sCode = outData.OUTDATA[0].EQPT_CTRL_STAT_CODE;
                EqpControlCmdSearchRequest(ClientPacket, txnID, sCode);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }

        protected virtual void EqpControlCmdSearchRequest(CPacket ClientPacket, string txnID, string code)
        {
            string msgID = "ECMD";
            string sSendBody = "";

            CECMD ecmd = null;

            if (SIMULATION_MODE)
            {
                #region Simul Case
                string dataSim = _EIFServer.GetJsonValue(typeof(CECMD));
                if (!string.IsNullOrEmpty(dataSim)) // ECMD 값이 없다면 처리할 필요가 없음
                {
                    ecmd = JsonConvert.DeserializeObject<CECMD>(dataSim);

                    if (ecmd == null) throw new Exception($"[{ecmd.ToString()}] : Simulation Data Not Exist!!");

                    ecmd.EqpID = ClientPacket.ObjectID;

                    if (!string.IsNullOrEmpty(ecmd.RCMD))
                    {
                        JObject json = new JObject();
                        json.Add(msgID, JObject.FromObject(ecmd));
                        SendHostMsg(ClientPacket.SoketClient, txnID, msgID, EX_REPLY, json.ToString());
                    }
                }
                #endregion
            }
            else
            {
                #region Real Case
                if (code == OPER_PAUSE || code == OPER_END || code == OPER_RESUME || code == OPER_RESTART || code == OPER_RESET || code == OPER_TRAY_UNLOAD || code == OPER_TRAY_RTOR)
                {
                    switch (code)
                    {
                        case OPER_PAUSE:
                            sSendBody = "P";
                            break;
                        case OPER_END:
                            sSendBody = "E";
                            break;
                        case OPER_RESUME:   //'S'
                            sSendBody = "R";       //$ 2020.06.23 : P -> R로 변경
                            break;
                        case OPER_RESTART:  //'Q'
                            sSendBody = "S";      //$ 2020.06.23 : R -> S로 변경
                            break;
                        case OPER_RESET:
                            sSendBody = "I";
                            break;
                        case OPER_TRAY_UNLOAD:  //'F'
                            sSendBody = "U";
                            break;
                        case OPER_TRAY_RTOR: //'F'
                            sSendBody = "L";     //$ 2025.07.16 : U -> L로 변경
                            break;
                    }

                    ecmd = new CECMD { EqpID = ClientPacket.ObjectID, RCMD = sSendBody }; //$ 2002.06.23 : EqpID가 상대 설비인지 확인 필요(이전 Log 확인 필)

                    JObject json = new JObject();
                    json.Add(msgID, JObject.FromObject(ecmd));

                    SendHostMsg(ClientPacket.SoketClient, txnID, msgID, EX_REPLY, json.ToString());
                }
                #endregion
            }
        }
        #endregion

        #region Common Method
        /// <summary> Cmd Parsing 및 CPacket Class에 바인딩</summary>
        private void ParseMsg(CPacket ClientPacket, string strMsg)
        {
            try
            {
                //$ 2019.05.16 : Const 변수로 Size 및 Index 계산하도록 수정
                int idx = 0;
                ClientPacket.Msg = strMsg;
                ClientPacket.Header = strMsg.Substring(idx, HEADERSIZE);
                ClientPacket.Dir = strMsg.Substring(idx, DIRSIZE);

                string rawObjectID = strMsg.Substring(idx += DIRSIZE, OBJECTIDSIZE); //$ 2021.09.29 : Log찍을때 Space를 명확하게 남기기 위해 추가
                ClientPacket.RawObjectID = rawObjectID;
                ClientPacket.ObjectID = rawObjectID.TrimEnd(); //$ 2021.07.14 : ObjectID 가변(최소8, 최대 19, Total 20) 실제 운영에서는 Space는 불필요하니 Trim 처리
                ClientPacket.Type = strMsg.Substring(idx += OBJECTIDSIZE, TYPESIZE);
                ClientPacket.Reply = strMsg.Substring(idx += TYPESIZE, REPLYSIZE);
                ClientPacket.MsgID = strMsg.Substring(idx += REPLYSIZE, MSGIDSIZE);
                ClientPacket.SeqNo = strMsg.Substring(idx += MSGIDSIZE, SEQNOSIZE);
                ClientPacket.Body = strMsg.Substring(idx += SEQNOSIZE);
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, this.HostObjID);
            }
        }

        /// <summary>Host Response </summary>        
		public void SendReplyMsg(CPacket ClientPacket, string txnID = "", bool bSucess = true, string addInfo = "")
        {
            try
            {
                if (ClientPacket.Reply.Equals(EX_REPLY))
                {
                    //Header 생성
                    ClientPacket.SendHeader = HOSTDIRECT + this.HostObjectID + REPLY + NO_REPLY + ClientPacket.SendMsgID + ClientPacket.SeqNo;
                    //Body 생성
                    ClientPacket.SyncBodyList();


                    //Byte 변경 후 Client 전송
                    ClientPacket.SoketClient.Send(ClientPacket.MakeStringToByteMsg(ClientPacket.SendHeader + ClientPacket.SendBody));
                    _EIFServer.SetLog("[" + ClientPacket.SoketClient.Id + "]" + "H MsgID: " + ClientPacket.SendMsgID + " SEQNO: " + ClientPacket.SeqNo + " REPLY: " + NO_REPLY + " HEADER: " + ClientPacket.SendHeader + " BODY: " + ClientPacket.SendBody, this.HostObjID, ClientPacket.ObjectID);

                    if (!string.IsNullOrEmpty(txnID)) // 2025.05.22 :
                    {
                        string msgID = string.IsNullOrEmpty(addInfo) ? ClientPacket.RecvMgsID : $"{ClientPacket.RecvMgsID} {addInfo}";
                        string result = bSucess ? "OK" : "NG";

                        SolaceLog(ClientPacket.ObjectID, txnID, 5, $"SEND {result} : {msgID} - {ClientPacket.SendHeader}");
                    }
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, ClientPacket.ObjectID);
            }
        }

        /// <summary>Client Request </summary>
        public void SendHostMsg(CSocketClient Client, string txnID, string sMSG, string sRpl, string sBody)
        {
            try
            {
                string SeqNo = GetNextSequence().ToString().PadLeft(4, '0');
                //Header 생성
                string SendHeader = HOSTDIRECT + this.HostObjectID + SEND + sRpl + sMSG.PadRight(6, ' ') + SeqNo;
                //Body 생성
                string SendBody = sBody;

                //Byte 변경 후 Client 전송
                Client.Send(MakeStringToByteMsg(SendHeader + SendBody));

                _EIFServer.SetLog("[" + Client.Id + "]" + "H MsgID: " + sMSG + " SEQNO: " + SeqNo + " REPLY: " + EX_REPLY + " HEADER: " + SendHeader + " BODY: " + SendBody, this.HostObjID, this.BASE.DicClientEqpID[Client.Id]);

                if (!string.IsNullOrEmpty(txnID)) // 2025.05.22 :
                {
                    SolaceLog(this.BASE.DicClientEqpID[Client.Id], txnID, 1, $"SEND : {sMSG} - {SendBody}");
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, Client.Id);
            }
        }

        /// <summary>Receive Message  Log</summary>
        public bool ReceiveMessageLogWrite(CPacket ClientPacket, string txnID, bool bMandatory = false, string ceid = "")
        {
            bool timeOutTest = false;

            try
            {
                _EIFServer.SetLog("[" + ClientPacket.SoketClient.Id + "]" + "E MsgID: " + ClientPacket.RecvMgsID + " SEQNO: " + ClientPacket.SeqNo + " REPLY: " + ClientPacket.Reply +
                                                                        " HEADER: " + ClientPacket.Dir + ClientPacket.RawObjectID + ClientPacket.Type + ClientPacket.Reply + ClientPacket.MsgID + ClientPacket.SeqNo +
                                                                        " BODY: " + ClientPacket.Body, this.HostObjID, ClientPacket.ObjectID);

                if (!string.IsNullOrEmpty(txnID)) // 2025.05.22 :
                {
                    string msgID = string.IsNullOrEmpty(ceid) ? ClientPacket.RecvMgsID : $"{ClientPacket.RecvMgsID} {ceid}";
                    SolaceLog(ClientPacket.ObjectID, txnID, 1, $"RECV : {msgID} - {ClientPacket.Dir + ClientPacket.RawObjectID + ClientPacket.Type + ClientPacket.Reply + ClientPacket.MsgID + ClientPacket.SeqNo}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                //$ 2023.01.05 : Timeout Test Mode 선택 시 설비로 Request 및 Reply 안함(단 필수 기능인 경우 예외 발생 안함)
                //$ 2023.05.16 : Nak Test시 1회 Host Trouble 이후 Retry시 정상 진행 됨
                if (!bMandatory && (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST))
                {
                    if (this.NakPassList == null) this.NakPassList = new Dictionary<string, bool>();
                    if (this.TimeOutPassList == null) this.TimeOutPassList = new Dictionary<string, bool>();

                    if (this.BASE.TESTMODE__V_IS_NAK_TEST && this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        string keyMsgID = $"{ClientPacket.RecvMgsID}{ceid}";

                        if (!this.NakPassList.ContainsKey(keyMsgID) || (this.NakPassList.ContainsKey(keyMsgID) && this.NakPassList[keyMsgID] == false))
                        {
                            this.NakPassList.Add(keyMsgID, true);
                            throw new Exception("EIF NAK Test Exception");
                        }

                        if (!this.TimeOutPassList.ContainsKey(keyMsgID) || (this.TimeOutPassList.ContainsKey(keyMsgID) && this.TimeOutPassList[keyMsgID] == false))
                        {
                            this.TimeOutPassList.Add(keyMsgID, true);
                            timeOutTest = true;
                        }
                    }
                    else if (this.BASE.TESTMODE__V_IS_NAK_TEST)
                    {
                        throw new Exception("EIF NAK Test Exception");
                    }
                    else if (this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                    {
                        timeOutTest = true;
                    }
                }
            }

            return timeOutTest;
        }

        /// <summary>Recive Command에 따른 Send Command </summary>
        public string PacketCheck(CPacket ClientPacket)
        {
            string sCode = "0";
            switch (ClientPacket.RecvMgsID)
            {
                case "ELNK":
                    ClientPacket.SendMsgID = "ELNK_R";
                    break;

                case "EAPD":
                    ClientPacket.SendMsgID = "EAPD_R";
                    break;

                case "EEER":
                    ClientPacket.SendMsgID = "EEER_R";
                    break;

                case "EERR_R":
                    break;

                case "ELSR":
                    ClientPacket.SendMsgID = "ELSR_R";
                    break;

                case "ERSR":
                    ClientPacket.SendMsgID = "ERSR_R";
                    break;

                case "ECMD":
                    ClientPacket.SendMsgID = "ECMD_R";
                    break;

                case "EALM":
                    ClientPacket.SendMsgID = "EALM_R";
                    break;

                case "ELIR":
                    ClientPacket.SendMsgID = "ELIR_R";
                    break;

                case "ECMD_R":
                    break;

                case "EDNT_R":
                    break;

                case "EUMR":
                    break;

                case "ESDR": //$ 2024.03.19 : 화재 관련 보고용 신규 MessageID
                    ClientPacket.SendMsgID = "ESDR_R";
                    break;

                default:
                    sCode = UNKNOWN_CMD_ERR;
                    break;
            }
            return sCode;
        }

        /// <summary>
        /// BizActor Solace 호출
        /// </summary>        
		private int BizCall(string bizName, string eqpID, CStructureVariable inVariable, CStructureVariable outVariable, out Exception bizEx, string txnID = "", bool bLogging = true)
        {
            bizEx = null;

            DateTime preTime = DateTime.Now;

            //$ 2025.05.14 : Solace Log는 tnxID와 eventName이 없는 경우 남기지 않음
            if (string.IsNullOrEmpty(txnID))
            {
                _EIFServer.EnableLoggingBizRule = bLogging;

                int nRst = (SIMULATION_MODE) ? 0 : _EIFServer.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BIZINFO__V_BIZCALL_TIMEOUT, out bizEx);

                _EIFServer.EnableLoggingBizRule = true;

                return nRst;
            }
            ;

            SolaceLog(eqpID, txnID, 2, $"{bizName} : {inVariable.Variable.ToString()}");

            SolaceLog(eqpID, txnID, 3, $"{bizName}"); // 2025.05.22 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

            _EIFServer.EnableLoggingBizRule = bLogging;

            int iRst = (SIMULATION_MODE) ? 0 : _EIFServer.RequestQueueBR_Variable(this.ReqQueue, this.RepQueue, bizName, inVariable, outVariable, this.BIZINFO__V_BIZCALL_TIMEOUT, out bizEx);

            _EIFServer.EnableLoggingBizRule = true;

            SolaceLog(eqpID, txnID, 3, $"{bizName} [{(DateTime.Now - preTime).TotalMilliseconds:0.0}ms] - {iRst}");

            if (!string.IsNullOrEmpty(txnID) && iRst == 0 && outVariable != null)
                SolaceLog(eqpID, txnID, 4, $"{bizName} : {outVariable.Variable.ToString()}");

            return iRst;
        }

        #endregion

        #region Custom Method
        object seqlock = new object();
        int sequence_no = 0;
        public int GetNextSequence()
        {
            lock (seqlock)
            {
                sequence_no += 1;

                if (sequence_no >= MAX_SEQ)
                {
                    sequence_no = 1;
                }

                return sequence_no;
            }
        }

        public string MakeStringToByteMsg(string sMsg)
        {
            int len = 0;
            byte STX = 0x02;
            byte ETX = 0x03;
            byte[] bTest = System.Text.Encoding.Default.GetBytes(sMsg);

            len = 1 + bTest.Length + 2 + 1;

            byte[] byteMessage = new byte[len];
            byte[] tmp;

            int off = 0;
            try
            {
                tmp = new byte[1];
                tmp[0] = STX;
                Buffer.BlockCopy(tmp, 0, byteMessage, off, tmp.Length);

                //Msg Data Add
                off += tmp.Length;
                Buffer.BlockCopy(bTest, 0, byteMessage, off, bTest.Length);

                // CheckSum Add
                byte[] b = System.Text.Encoding.Default.GetBytes((GetCheckSum(bTest)));
                off += bTest.Length;
                Buffer.BlockCopy(b, 0, byteMessage, off, b.Length);

                tmp = new byte[1];
                tmp[0] = ETX;
                off += b.Length;
                Buffer.BlockCopy(tmp, 0, byteMessage, off, tmp.Length);
                return System.Text.Encoding.Default.GetString(byteMessage);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public string GetCheckSum(byte[] b)
        {
            int v = 0;
            try
            {

                for (int i = 0; i < b.Length; i++)
                {
                    v += b[i];

                    //System.Diagnostics.Trace.WriteLine(string.Format("{0} : {1}", i+1, v));
                }
                string strTmp = v.ToString();

                //
                return strTmp.Substring(strTmp.Length - 2);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }
        #endregion


        #region Common Packet Class
        public class CPacket
        {
            #region Attribute Persistance

            private string m_CheckSum;
            public string CheckSum
            {
                get
                {
                    return m_CheckSum;
                }
                set
                {
                    m_CheckSum = value;
                }
            }

            private string m_Msg;
            public string Msg
            {
                get
                {
                    return m_Msg;
                }
                set
                {
                    m_Msg = value;
                }
            }

            private string m_Header;
            public string Header
            {
                get
                {
                    return m_Header;
                }
                set
                {
                    m_Header = value;
                }
            }

            private string m_Dir;
            public string Dir
            {
                get
                {
                    return m_Dir;
                }
                set
                {
                    m_Dir = value;
                }
            }

            private string m_ObjectID;
            public string ObjectID
            {
                get
                {
                    return m_ObjectID;
                }
                set
                {
                    m_ObjectID = value;
                }
            }

            private string m_RawObjectID;
            public string RawObjectID //$ 2021.09.29 : Log찍을때 Space를 명확하게 남기기 위해 추가
            {
                get
                {
                    return m_RawObjectID;
                }
                set
                {
                    m_RawObjectID = value;
                }
            }

            private string m_Reply;
            public string Reply
            {
                get
                {
                    return m_Reply;
                }
                set
                {
                    m_Reply = value;
                }
            }

            private string m_Type; //$ 2020.06.01 : 공상평 표준 사양
            public string Type
            {
                get
                {
                    return m_Type;
                }
                set
                {
                    m_Type = value;
                }
            }

            private string m_MsgID; //$ 2020.06.01 : 공상평 표준 사양 CmdID -> MsgID
            public string MsgID
            {
                get
                {
                    return m_MsgID;
                }
                set
                {
                    m_MsgID = value;
                }
            }

            //$ 2020.06.08 : MessageID를 Trim 쳐서 두루두루 쓸때가 많을 것으로 예상되어 속성 추가
            public string RecvMgsID
            {
                get
                {
                    return m_MsgID.Trim();
                }
            }

            private string m_SeqNo;
            public string SeqNo
            {
                get
                {
                    return m_SeqNo;
                }
                set
                {
                    m_SeqNo = value;
                }
            }

            private string m_BoxID;  //$ 2020.06.01 : 삭제 예정
            public string BoxID
            {
                get
                {
                    return m_BoxID;
                }
                set
                {
                    m_BoxID = value;
                }
            }

            private string m_Body;
            public string Body
            {
                get
                {
                    return m_Body;
                }
                set
                {
                    m_Body = value;
                }
            }

            private string m_SendMsgID;
            public string SendMsgID
            {
                get
                {
                    return m_SendMsgID;
                }
                set
                {
                    m_SendMsgID = value;
                }
            }

            private string m_SendHeader;
            public string SendHeader
            {
                get
                {
                    return m_SendHeader;
                }
                set
                {
                    m_SendHeader = value;
                }
            }

            private string m_SendBody;
            public string SendBody
            {
                get
                {
                    return m_SendBody;
                }
            }

            private Hashtable m_SendBodyList;
            public Hashtable SendBodyList
            {
                get
                {
                    return m_SendBodyList;
                }
            }

            public CSocketClient SoketClient = null;
            #endregion

            #region Constructor
            public CPacket(CSocketClient Client)
            {
                this.SoketClient = Client;
                m_SendBodyList = new Hashtable();
            }
            public CPacket(string sPacket)
            {
                //$ 2019.05.16 : Const 변수로 Size 및 Index 계산하도록 수정
                int idx = 0;
                this.Dir = sPacket.Substring(idx, DIRSIZE);
                this.ObjectID = sPacket.Substring(idx += DIRSIZE, OBJECTIDSIZE).TrimEnd(); //$ 2021.07.14 : ObjectID 가변(최소8, 최대 19, Total 20) 실제 운영에서는 Space는 불필요하니 Trim 처리
                this.Reply = sPacket.Substring(idx += OBJECTIDSIZE, REPLYSIZE);
                this.Type = sPacket.Substring(idx += REPLYSIZE, TYPESIZE);
                this.MsgID = sPacket.Substring(idx += TYPESIZE, MSGIDSIZE);
                this.SeqNo = sPacket.Substring(idx += MSGIDSIZE, SEQNOSIZE);
                this.Body = sPacket.Substring(idx += SEQNOSIZE);
            }
            #endregion

            #region Common Method
            public string MakeStringToByteMsg(string sMsg)
            {
                int len = 0;
                byte STX = 0x02;
                byte ETX = 0x03;
                byte[] bTest = System.Text.Encoding.Default.GetBytes(sMsg);

                len = 1 + bTest.Length + 2 + 1;

                byte[] byteMessage = new byte[len];
                byte[] tmp;

                int off = 0;
                try
                {
                    tmp = new byte[1];
                    tmp[0] = STX;
                    Buffer.BlockCopy(tmp, 0, byteMessage, off, tmp.Length);

                    //Msg Data Add
                    off += tmp.Length;
                    Buffer.BlockCopy(bTest, 0, byteMessage, off, bTest.Length);

                    // CheckSum Add
                    byte[] b = System.Text.Encoding.Default.GetBytes((GetCheckSum(bTest)));
                    off += bTest.Length;
                    Buffer.BlockCopy(b, 0, byteMessage, off, b.Length);

                    tmp = new byte[1];
                    tmp[0] = ETX;
                    off += b.Length;
                    Buffer.BlockCopy(tmp, 0, byteMessage, off, tmp.Length);
                    return System.Text.Encoding.Default.GetString(byteMessage);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

            public void SyncBodyList()
            {
                string sBody = null;
                for (int i = 0; i < SendBodyList.Count; i++)
                {
                    sBody += SendBodyList[((int)(i + 1)).ToString()].ToString();
                }

                m_SendBody = sBody;
            }

            public string GetCheckSum(byte[] b)
            {
                int v = 0;
                try
                {

                    for (int i = 0; i < b.Length; i++)
                    {
                        v += b[i];
                    }
                    string strTmp = v.ToString();
                    return strTmp.Substring(strTmp.Length - 2);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

            public string GetCheckSum(string sMsg)
            {
                byte[] b = System.Text.Encoding.Default.GetBytes(sMsg);
                int v = 0;
                for (int i = 0; i < b.Length; i++)
                {
                    v += b[i];
                }
                string strTmp = v.ToString();
                return strTmp.Substring(strTmp.Length - 2);
            }

            public bool GetSizeCheck(int MaxSize)
            {
                bool bRet = false;
                if (Msg.Length == MaxSize)
                    bRet = true;
                return bRet;
            }

            #endregion
        }
        #endregion

        #region Solace Log Method // 2025.05.22 : PC Type Solace Elastic Search Log 전송용
        public void SolaceLog(string eqpID, string txnID, int stepNo, string message)
        {
            _EIFServer.SetSolLog(eqpID, txnID, stepNo, message); // 2025.05.22 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
        }
        #endregion

        //$ 2025.07.16 : 관제 시스템으로 보고용 Method 추가 (불필요 코드 정리)
        private void SmokeReport(string eqpID, string smokeStatus)
        {
            try
            {
                #region Json Message 생성
                CBaseSmoke sstr = new CBaseSmoke
                {
                    TXN_ID = this.SendTnxID,
                    ActID = "SmokeStatusTransfer",
                    InDataName = "IN_DATA",
                    OutDataName = "",
                    refds = new CSSTR()
                };

                var sstrData = (sstr.refds as CSSTR);
                sstrData.InDataList = new CSSTRInData[1];

                CSSTRInData subData = new CSSTRInData
                {
                    EqpNickName = "EIF_01",
                    EqpID = eqpID,
                    SmokeStatus = smokeStatus
                };

                sstrData.InDataList.SetValue(subData, 0);
                #endregion

                #region 관제 Solace 전송 - channel은 미리 만들지만, 연결은 계속 유지 하지 않고 전송 후 닫음(자주 전송될 메시지가 아님)
                string rcvMessage = string.Empty;
                JObject json = JObject.FromObject(sstr);

                //$ 2025.07.18 : Smoke 관제 보고 관련 Solace 연결은 첫 연기 감지 보고때만 Connect 함
                if (this.SmokeSolChannel.ConnectionState != enumConnectionState.Connected)
                    this.SmokeSolChannel.Connect();

                int result = SmokeSolChannel.SendQueue(this.SMOKE_SOLACE__V_REQQUEUE_NAME, json.ToString());

                if (result == 0)
                    _EIFServer.SetLog(json.ToString(), this.HostObjID, $"Smoke Queue OK : {this.SMOKE_SOLACE__V_REQQUEUE_NAME}");
                else
                    _EIFServer.SetLog(json.ToString(), this.HostObjID, $"Smoke Queue NG : {this.SMOKE_SOLACE__V_REQQUEUE_NAME}");

                //$ 2025.07.18 : 연기 감지 보고 후 Channel을 Close하려고 하였으나 EIF가 아닌 Window Kernel에서 에러가 나는 현상이 있어 Session을 닫지 않음, 
                //               JF의 경우 18Box가 동시에 화재 보고를 하면서 문제가 있는듯하여 하기와 같이 조치 또한 ms단위 보고를 막기위해 Wait을 100ms 추가
                //this.SmokeSolChannel.Close();
                Wait(100);
                #endregion
            }
            catch (Exception ex)
            {
                _EIFServer.SetExceptLog(ex.ToString(), this.HostObjID, "SMOKE");
            }
        }

        #region Mointoring
        private void EIFMonitoringData()
        {
            this.MONITOR__V_MONITOR_SOLACE = this.BASE.ConnectionState.ToString();

            //$ 2025.07.28 : PC Type의 경우 PLC를 사용하지 않지만 연결된 설비가 1개라도 있다면 Online으로 표시
            if (this.BASE.DicClientEqpID.Count > 0)
                this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.ONLINE.ToString();
            else
                this.MONITOR__V_MONITOR_PLC_COMMNUICATION = PLCConnectionState.OFFLINE.ToString();
        }
        #endregion
    }
}