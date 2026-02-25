using System;
using System.Collections.Generic;

using LGCNS.ezControl.Core;
using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.JIGLDULD
{
    public partial class CJIGLDULD_BIZ
    {
        #region LD Input Station Available
        protected virtual void __T1_1_PORT_STAT_CHG_01__I_W_INPUT_ST_AVAIL_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                //JH 2024.03.27 TryParse의 경우 숫자형태 문자열의 결과가 항상 True 반환하여 이를 수정 
                ePortState eValue = (ePortState)value;
                if (!Enum.IsDefined(typeof(ePortState), eValue))
                {
                    //등록되지 않은 값이 올 경우 Log 찍고 빠짐
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{value} ===> Not Regist Value]");
                    return;
                }

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{eValue}]");

                _EIFServer.MhsReport_PortStatus(SIMULATION_MODE, sender, this.EQPID, this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, eValue, string.Empty, eCarrierType.U); //$ 2023.01.10 : Common Method변경
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region Empty Input Station Available
        protected virtual void __T1_1_PORT_STAT_CHG_03__I_W_INPUT_ST_AVAIL_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                //JH 2024.03.27 TryParse의 경우 숫자형태 문자열의 결과가 항상 True 반환하여 이를 수정 
                ePortState eValue = (ePortState)value;
                if (!Enum.IsDefined(typeof(ePortState), eValue))
                {
                    //등록되지 않은 값이 올 경우 Log 찍고 빠짐
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{value} ===> Not Regist Value]");
                    return;
                }

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{eValue}]");

                _EIFServer.MhsReport_PortStatus(SIMULATION_MODE, sender, this.EQPID, this.EQP_INFO__V_W_EMPTY_INPUT_STATION_EQP_ID, eValue, string.Empty, eCarrierType.E); //$ 2023.01.10 : Common Method변경
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_EMPTY_INPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region Empty Output Station Tray Exist
        protected virtual void __G2_1_CARR_ID_RPT_02__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, sender.Description + $" : [{(value ? "ON" : "OFF")}]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region Empty Output Station Tray ID Check Request
        protected virtual void __G2_1_CARR_ID_RPT_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_1_CARR_ID_RPT_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_1_CARR_ID_RPT_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_1_CARR_ID_RPT_02__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, sender.Description + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"Empty Output Station Tray ID Confirm : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

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

                if (!this.BASE.G2_1_CARR_ID_RPT_02__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"Invalid EQP Status!! Empty Output Station Tray : [{(this.BASE.G2_1_CARR_ID_RPT_02__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayId = string.Empty;
                ushort trayStep = IsHR ? this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_CARRY_CNT : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_CARRY_CNT); //적재단수

                string lowerTrayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_LOWER : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_LOWER);
                string upperTrayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_UPPER : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_UPPER);

                //기본 Validation
                //공Tray 출고Station Tray적재단수 체크
                if (trayStep == 0 || trayStep > 2)
                {
                    HostAlarm(sender, EIFALMCD.JIGTRAYCNT.ToString(), $"EMPTY_OUTPUT_ST_Invalid Tray Count : {trayStep}", 4);

                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"Invalid Tray Step : Tray Step=[{trayStep}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }
                else
                {
                    //공Tray 출고Station TrayID Empty 체크
                    for (ushort i = 0; i < trayStep; i++)
                    {
                        if (i == 0) // HDH 2023.04.17 : Trayid Lower, Upper 분리
                            trayId = lowerTrayID;
                        else if (i == 1)
                            trayId = upperTrayID;

                        if (string.IsNullOrEmpty(trayId))
                        {
                            HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"EMPTY_OUTPUT_ST_Tray ID is Null or Empty", 4);

                            string log = $"HostTrouble=Tray ID is Null or Empty, Tray Step=[{trayStep}], Tray1=[{lowerTrayID}], Tray2=[{upperTrayID}]";
                            _EIFServer.SetVarStatusLog(this.EQPID, sender, log);

                            //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                            HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                            return;
                        }
                    }
                }

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_LOWER))} : {lowerTrayID}, {GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_ID_UPPER))} : {upperTrayID}");

                int iRst = 0;
                Exception bizEx = null;
                string strBizName = "BR_GET_JIG_FORM_TRAY_EMPTY_CHECK";

                //BizRule 입력데이터 설정
                //$ 2022.07.21 : 잘못된 for문 종료 처리로 Biz를 2번 호출하는 경우가 있어 이를 1번 호출로 변경(Biz가 2개 Indata 처리가 가능함)
                CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_IN inData = CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_IN.GetNew(this);
                CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_OUT outData = CBR_GET_JIG_FORM_TRAY_EMPTY_CHECK_OUT.GetNew(this);
                inData.INDATA_LENGTH = 0;

                string trayIDs = string.Empty;
                for (ushort i = 0; i < trayStep; i++) //적재단수만큼 BizRule 호출처리
                {
                    if (i == 0) // HDH 2023.04.17 : Trayid Lower, Upper 분리
                        trayId = lowerTrayID;
                    else if (i == 1)
                        trayId = upperTrayID;

                    inData.INDATA_LENGTH++;

                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayId;

                    if (string.IsNullOrEmpty(trayIDs))
                        trayIDs = trayId;
                    else
                        trayIDs += $", {trayId}";
                }

                //입력 데이터가 없으면
                if (inData.INDATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"{strBizName} No Input Data!!");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule Call
                iRst = BizCall(strBizName, inData, outData, out bizEx);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 0);
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 4);

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                //BizRule 정상 처리 후
                if (outData.OUTDATA[0].RETVAL == 0)
                {
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"{strBizName} Success : Tray Step=[{trayStep}], Tray IDs = [{trayIDs}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"{strBizName} Fail :Tray Step=[{trayStep}], Tray IDs = [{trayIDs}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, "Empty Output Station Tray ID Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_1_CARR_ID_RPT_02__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_1_CARR_ID_RPT_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region Empty Output Station Available
        protected virtual void __T1_1_PORT_STAT_CHG_02__I_W_OUTPUT_ST_AVAIL_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                //JH 2024.03.27 TryParse의 경우 숫자형태 문자열의 결과가 항상 True 반환하여 이를 수정 
                ePortState eValue = (ePortState)value;
                if (!Enum.IsDefined(typeof(ePortState), eValue))
                {
                    //등록되지 않은 값이 올 경우 Log 찍고 빠짐
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{value} ===> Not Regist Value]");
                    return;
                }

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, sender.Description + $" : [{eValue}]");

                ushort trayStep = IsHR ? this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_CARRY_CNT : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_02__I_W_TRAY_CARRY_CNT); //적재단수

                //List<string> trayIDs = IsHR ? this.BASE.INOUT__I_W_EMPTY_TRAY_OUTPUT_ST_TRAYIDS : this.GetSimLists(() => this.BASE.INOUT__I_W_EMPTY_TRAY_OUTPUT_ST_TRAYIDS);

                // HDH 2023.04.17 : Trayid Lower, Upper 분리
                string[] trayIds_Temp = new string[trayStep];
                string trayId = string.Empty;

                for (ushort i = 0; i < trayStep; i++) //적재단수만큼 BizRule 호출처리
                {
                    if (i == 0)
                        trayId = IsHR ? this.BASE.T1_1_PORT_STAT_CHG_02__I_W_OUTPUT_ST_TRAY_ID_LOWER : _EIFServer.GetSimValue(() => this.BASE.T1_1_PORT_STAT_CHG_02__I_W_OUTPUT_ST_TRAY_ID_LOWER);
                    else if (i == 1)
                        trayId = IsHR ? this.BASE.T1_1_PORT_STAT_CHG_02__I_W_OUTPUT_ST_TRAY_ID_UPPER : _EIFServer.GetSimValue(() => this.BASE.T1_1_PORT_STAT_CHG_02__I_W_OUTPUT_ST_TRAY_ID_UPPER);

                    trayIds_Temp[i] = trayId;
                }

                List<string> trayIDs = new List<string>(trayIds_Temp);

                //기본 Validation
                //공Tray 출고Station Tray적재단수 체크
                if (eValue == ePortState.UR)
                {
                    if (trayStep == 0 || trayStep > 2)
                    {
                        HostAlarm(sender, EIFALMCD.JIGTRAYCNT.ToString(), $"EMPTY_OUTPUT_ST_Invalid Tray Count : {trayStep}", 4);

                        _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"Invalid Tray Step : Tray Step=[{trayStep}]");
                        return;
                    }
                    else
                    {
                        //공Tray 출고Station TrayID Empty 체크
                        for (ushort i = 0; i < trayStep; i++)
                        {
                            //string trayId = trayIDs[i];

                            if (string.IsNullOrEmpty(trayId))
                            {
                                HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"EMPTY_OUTPUT_ST_Tray ID is Null or Empty", 4);

                                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"HostTrouble=Tray ID is Null or Empty, Tray Step=[{trayStep}], Tray1=[{trayIDs[0]}], Tray2=[{trayIDs[1]}");
                                return;
                            }
                        }
                    }

                    //Jig 적재 정보 보고 : 공 Tray 배출   
                    string strBizName = string.Empty;
                    Exception ex = _EIFServer.MhsReport_StackStatus(SIMULATION_MODE, this.EQPID, this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, trayStep, trayIDs, eCarrierType.E, out strBizName); //$ 2023.01.10 : Common Method변경
                    if (ex != null)
                    {
                        HostBizAlarm(strBizName, this.EQPID, sender, ex, 4);
                        _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> Report Tray Stack Exception Occur!!");
                        return;
                    }
                }

                //Jig Port 상태 변경
                string lowerTrayID = trayIDs.Count > 0 ? trayIDs[0] : string.Empty; //$ 2023.06.06 : Lower TrayID 정보 추출, Tray가 없을 경우 string.empty
                _EIFServer.MhsReport_PortStatus(SIMULATION_MODE, sender, this.EQPID, this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, eValue, lowerTrayID, eCarrierType.E); //$ 2023.01.10 : Common Method변경
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region 공 Tray 적재기 //$ 2023.01.30 : 적재기 관련 내역 추가
        protected virtual void __S7_5_TRAY_TRNSINFO_01__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__I_B_TRAY_EXIST)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        protected virtual void __S7_5_TRAY_TRNSINFO_01__I_B_STACKED_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__I_B_STACKED_TRAY_EXIST)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        protected virtual void __S7_5_TRAY_TRNSINFO_01__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_ACTION_CODE);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2024.02.14 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                    if (bOccur) return;
                }

                string portID = this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID;
                string trayID = IsHR ? this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_TRAY_ID);
                string stackTrayID = IsHR ? this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_STACKED_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_STACKED_TRAY_ID);

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_TRAY_ID))} : {trayID}, {GetDesc(nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__I_W_STACKED_TRAY_ID))} : {stackTrayID}");

                Int32 iRst = 0;
                string strBizName = "BR_MHS_RequestStackCommand";
                Exception bizEx = null;

                CBR_MHS_RequestStackCommand_IN inData = CBR_MHS_RequestStackCommand_IN.GetNew(this);
                CBR_MHS_RequestStackCommand_OUT outData = CBR_MHS_RequestStackCommand_OUT.GetNew(this);

                inData.IN_DATA_LENGTH = 1;

                //$ 2023.12.27 : UC2에서 MHS Biz에 USERID가 추가된 내역에 대해 추가 반영(기본 3개 항목중 USERID만추가 되었음.. 이상함.. 구조를 맞추기 위해 SRCTYPE 및 IFMODE는 반영 후 주석)
                //inData.IN_DATA[inData.IN_DATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                //inData.IN_DATA[inData.IN_DATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].EQUIPMENT_ID = this.EQPID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].PORT_ID = portID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].DURABLE_ID = trayID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].DURABLE_STATUS_CODE = eCarrierType.E.ToString();

                inData.IN_STACKER_LIST_LENGTH = 0;
                if (!string.IsNullOrEmpty(stackTrayID))
                {
                    inData.IN_STACKER_LIST_LENGTH = 1;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].EQUIPMENT_ID = this.EQPID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].PORT_ID = portID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].DURABLE_ID = stackTrayID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].DURABLE_STATUS_CODE = eCarrierType.E.ToString();
                }

                // 입력 데이터가 없으면.
                if (inData.IN_DATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"LD Tray BCR Read Req No Input Data!! {this.EQPID} {trayID}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                // BizRule Call -> Current Inspector Tray#1 Arrived
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST {eventName} -> Report Tray Stack Command Exception Occur!!");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 4);

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                string sLog = string.Empty;
                if (IsRl)
                {
                    eStackCmd retVal = (ushort)eStackCmd.DEFAULT;
                    string resultCmd = outData.OUT_DATA[0].STACK_COMMAND;

                    //BizRule Call -> 적재기 Action Code (1.적재 2.배출 3.통과) , Tray Stack Count 요청.
                    if (resultCmd == STACKCMD.STACK || resultCmd == STACKCMD.STACKWAIT) // 적재
                        retVal = eStackCmd.STACK;
                    else
                        retVal = eStackCmd.REPLACE;

                    if (retVal != 0)
                    {
                        this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_STACK_COUNT = 2; //IsRl ? outData.OUTDATA[0].STACK_CNT : _EIFServer.GetSimValue(() => this.STACK_TRAY_INFO__O_W_LOADER_STACK_COUNT);
                        this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_WAIT_TIME = this.BASE.STACK_TRAY_INFO__V_W_LOADER_WAIT_TIME;
                        this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_BCR_READ_COUNT = this.BASE.STACK_TRAY_INFO__V_W_LOADER_BCR_READ_COUNT;

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, (eConfirm)retVal, txnID);

                        sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF} - {resultCmd}";
                        _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                    }
                    else
                    {
                        sLog = $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF} - {resultCmd}";
                        _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    }
                }
                else
                {
                    ushort retVal = _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_ACTION_CODE);
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, (eConfirm)retVal, txnID);

                    sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF} - {retVal} : 1 - STACK, 2 - REPLACE";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.S7_5_TRAY_TRNSINFO_01__O_W_ACTION_CODE = (ushort)eConfirm.NAK;
                this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        protected virtual void __S7_5_TRAY_TRNSINFO_01__O_B_BCR_RETRY_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_01__O_B_BCR_RETRY)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{System.Reflection.MethodBase.GetCurrentMethod().Name} - BCR Read Retry Host Trouble");
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion

        #region ULD Output Station Tray Exist
        protected virtual void __G2_1_CARR_ID_RPT_03__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, sender.Description + $" : [{(value ? "ON" : "OFF")}]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region ULD Output Station Tray ID Check Request
        protected virtual void __G2_1_CARR_ID_RPT_03__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.G2_1_CARR_ID_RPT_03__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.G2_1_CARR_ID_RPT_03__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.G2_1_CARR_ID_RPT_03__O_W_TRIGGER_REPORT_ACK);

                _EIFServer.SetVarStatusLog(this.EQPID, sender, sender.Description + $" : [{(value ? "ON" : "OFF")}]");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, "ULD Output Station Tray ID Confirm : [OFF]");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //EQP 상태 확인
                //$ 2023.01.05 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE || this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    if (!this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE)
                    {
                        _EIFServer.SetVarStatusWarnLog(this.Name, sender, $"OperationMode : {(this.BASE.EQP_OP_MODE_CHG_RPT__I_B_AUTO_MODE ? "CONTROL" : "MAINTENANCE")}");

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

                if (!this.BASE.G2_1_CARR_ID_RPT_03__I_B_TRAY_EXIST)
                {
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"Invalid EQP Status!! ULD Output Station Tray : [{(this.BASE.G2_1_CARR_ID_RPT_03__I_B_TRAY_EXIST ? "EXIST" : "EMPTY")}]"); //LHS 2023.08.04 : 태그 오류 수정

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                string trayId = string.Empty;
                ushort trayStep = IsHR ? this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_CARRY_CNT : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_CARRY_CNT); //적재단수

                string lowerTrayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_LOWER : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_LOWER);
                string upperTrayID = IsHR ? this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_UPPER : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_UPPER);

                //기본 Validation
                //실Tray 출고Station Tray적재단수 체크
                if (trayStep == 0 || trayStep > 2)
                {
                    HostAlarm(sender, EIFALMCD.JIGTRAYCNT.ToString(), $"ULD_TRAY_OUTPUT_ST_Invalid Tray Count : {trayStep}", 7);

                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"Invalid Tray Step : Tray Step=[{trayStep}]");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }
                else
                {
                    //실Tray 출고Station TrayID Empty 체크
                    for (ushort i = 0; i < trayStep; i++)
                    {
                        if (i == 0) // HDH 2023.04.17 : Trayid Lower, Upper 분리
                            trayId = lowerTrayID;
                        else if (i == 1)
                            trayId = upperTrayID;

                        if (string.IsNullOrEmpty(trayId))
                        {
                            HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"ULD_TRAY_OUTPUT_ST_Tray ID is Null or Empty", 7);

                            string log = $"HostTrouble=Tray ID is Null or Empty, Tray Step=[{trayStep}], Tray1=[{lowerTrayID}], Tray2=[{upperTrayID}]";

                            _EIFServer.SetVarStatusLog(this.EQPID, sender, log);

                            //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                            HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                            return;
                        }
                    }
                }

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_LOWER))} : {lowerTrayID}, {GetDesc(nameof(this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_ID_UPPER))} : {upperTrayID}");

                //BizRule
                int iRst = 0;
                Exception bizEx = null;
                string strBizName = "BR_SET_JIG_FORM_ULD_OUTPUT_TRAY_CHECK";

                List<Tuple<string, string, string>> routeLotIDList = new List<Tuple<string, string, string>>();
                bool isSuccess = false;

                CBR_SET_JIG_FORM_ULD_OUTPUT_TRAY_CHECK_IN inData = CBR_SET_JIG_FORM_ULD_OUTPUT_TRAY_CHECK_IN.GetNew(this);
                CBR_SET_JIG_FORM_ULD_OUTPUT_TRAY_CHECK_OUT outData = CBR_SET_JIG_FORM_ULD_OUTPUT_TRAY_CHECK_OUT.GetNew(this);

                for (ushort i = 0; i < trayStep; i++) //적재단수만큼 BizRule 호출처리(TrayCheck 공통 BizRule 1건만 처리가능)
                {
                    if (i == 0) // HDH 2023.04.17 : Trayid Lower, Upper 분리
                        trayId = lowerTrayID;
                    else if (i == 1)
                        trayId = upperTrayID;

                    //BizRule 입력데이터 설정
                    inData.INDATA_LENGTH = 1;
                    inData.INDATA[inData.INDATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                    inData.INDATA[inData.INDATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                    inData.INDATA[inData.INDATA_LENGTH - 1].USERID = USERID.EIF;

                    inData.INDATA[inData.INDATA_LENGTH - 1].EQPTID = this.EQPID;
                    inData.INDATA[inData.INDATA_LENGTH - 1].CSTID = trayId;

                    //입력 데이터가 없으면
                    if (inData.INDATA.Count == 0)
                    {
                        _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{strBizName} No Input Data!!");

                        //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                        return;
                    }

                    //BizRule Call
                    iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                    if (iRst != 0)
                    {
                        _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                        _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                        _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST NAK {strBizName} - Biz Exception = {iRst}");

                        this.HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 7);

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                        return;
                    }

                    //BizRule 성공로그
                    _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                    string RouteID = outData.OUTDATA[0].ROUTID;
                    string GroupLotID = outData.OUTDATA[0].DAY_GR_LOTID;
                    string SpecialInfo = outData.OUTDATA[0].SPECIALINFO;

                    var routeLotID = new Tuple<string, string, string>(RouteID, GroupLotID, SpecialInfo);
                    _EIFServer.SetVarStatusLog(sender, $"ULD Output Station Route_Lot_ID[{i}]=[{routeLotID}]");

                    //BizRule 정상 처리 후
                    if (true /*outData.OUTDATA[0].RETVAL.Equals("0")*/) //TODO $ 2020.11.25 : RETVAL 누락으로 일단 주석 처리함
                    {
                        routeLotIDList.Add(routeLotID);
                        _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{strBizName} Success : Tray Step=[{trayStep}], Tray ID[{i}]=[{trayId}], Route LOT ID[{i}]=[{routeLotID}]");
                        isSuccess = true;
                    }
                    else
                    {
                        _EIFServer.SetVarStatusLog(this.EQPID, sender, $"{strBizName} Fail :Tray Step=[{trayStep}], Tray ID[{i}]=[{trayId}], Route LOT ID[{i}]=[{routeLotID}]");
                        isSuccess = false;
                    }
                }

                if (isSuccess)
                {
                    #region TrayID에 해당하는 Route, Lot, Special Info를 Write
                    int i = 0;
                    foreach (var routeLot in routeLotIDList)
                    {
                        if (i == 0)
                        {
                            //$ 2021.07.20 : GroupLotID를 8자리만 준다면 substring은 빼자!!
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_ROUTE_ID_1ST = IsRl ? routeLot.Item1 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_ROUTE_ID_1ST);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_GROUP_LOT_ID_1ST = IsRl ? routeLot.Item2.Substring(0, 8) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_GROUP_LOT_ID_1ST);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_INFO_1ST = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_INFO_1ST);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_NO_1ST = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_NO_1ST);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_LOT_ID_1ST = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_LOT_ID_1ST);
                        }
                        else if (i == 1)
                        {
                            //$ 2021.07.20 : GroupLotID를 8자리만 준다면 substring은 빼자!!
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_ROUTE_ID_2ND = IsRl ? routeLot.Item1 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_ROUTE_ID_2ND);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_GROUP_LOT_ID_2ND = IsRl ? routeLot.Item2.Substring(0, 8) : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_GROUP_LOT_ID_2ND);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_INFO_2ND = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_INFO_2ND);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_NO_2ND = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_SPECIAL_NO_2ND);
                            this.BASE.G2_1_CARR_ID_RPT_03__O_W_LOT_ID_2ND = IsRl ? routeLot.Item3 : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__O_W_LOT_ID_2ND);
                        }

                        i++;
                    }
                    #endregion                    

                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"{strBizName} Success :Tray Step=[{trayStep}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.ACK, txnID);
                }
                else
                {
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_EMPTY_OUTPUT_STATION_EQP_ID, sender, $"{strBizName} Fail :Tray Step=[{trayStep}]");

                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                }

                _EIFServer.SetVarStatusLog(this.EQPID, sender, $"ULD Output Station Tray ID Confirm : [ON]");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQPID, sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                this.BASE.G2_1_CARR_ID_RPT_03__O_W_TRIGGER_REPORT_ACK = (ushort)eConfirm.NAK;
                this.BASE.G2_1_CARR_ID_RPT_03__O_B_TRIGGER_REPORT_CONF = true;
            }
        }
        #endregion

        #region ULD Output Station Available
        protected virtual void __T1_1_PORT_STAT_CHG_04__I_W_OUTPUT_ST_AVAIL_OnShortChanged(CVariable sender, ushort value)
        {
            try
            {
                //JH 2024.03.27 TryParse의 경우 숫자형태 문자열의 결과가 항상 True 반환하여 이를 수정 
                ePortState eValue = (ePortState)value;
                if (!Enum.IsDefined(typeof(ePortState), eValue))
                {
                    //등록되지 않은 값이 올 경우 Log 찍고 빠짐
                    _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_LD_INPUT_STATION_EQP_ID, sender, sender.Description + $" : [{value} ===> Not Regist Value]");
                    return;
                }

                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, sender.Description + $" : [{eValue}]");

                //적재단수
                ushort trayStep = IsHR ? this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_CARRY_CNT : _EIFServer.GetSimValue(() => this.BASE.G2_1_CARR_ID_RPT_03__I_W_TRAY_CARRY_CNT);

                //List<string> trayIDs = IsHR ? this.BASE.INOUT__I_W_ULD_TRAY_OUTPUT_ST_TRAYIDS : this.GetSimLists(() => this.BASE.INOUT__I_W_ULD_TRAY_OUTPUT_ST_TRAYIDS);

                // HDH 2023.04.17 : Trayid Lower, Upper 분리
                string[] trayIds_Temp = new string[trayStep];
                string trayId = string.Empty;

                for (ushort i = 0; i < trayStep; i++) //적재단수만큼 BizRule 호출처리
                {
                    if (i == 0)
                        trayId = IsHR ? this.BASE.T1_1_PORT_STAT_CHG_04__I_W_OUTPUT_ST_TRAY_ID_LOWER : _EIFServer.GetSimValue(() => this.BASE.T1_1_PORT_STAT_CHG_04__I_W_OUTPUT_ST_TRAY_ID_LOWER);
                    else if (i == 1)
                        trayId = IsHR ? this.BASE.T1_1_PORT_STAT_CHG_04__I_W_OUTPUT_ST_TRAY_ID_UPPER : _EIFServer.GetSimValue(() => this.BASE.T1_1_PORT_STAT_CHG_04__I_W_OUTPUT_ST_TRAY_ID_UPPER);

                    trayIds_Temp[i] = trayId;
                }

                List<string> trayIDs = new List<string>(trayIds_Temp);

                //기본 Validation
                //실Tray 출고Station Tray적재단수 체크
                if (eValue == ePortState.UR)
                {
                    if (trayStep == 0 || trayStep > 2)
                    {
                        HostAlarm(sender, EIFALMCD.JIGTRAYCNT.ToString(), $"TRAY_OUTPUT_ST_Invalid Tray Count : {trayStep}", 7);

                        _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, $"Invalid Tray Step : Tray Step=[{trayStep}]");
                        return;
                    }
                    else
                    {
                        //실Tray 출고Station TrayID Empty 체크
                        for (ushort i = 0; i < trayStep; i++)
                        {
                            //string trayId = trayIDs[i];

                            if (string.IsNullOrEmpty(trayId))
                            {
                                HostAlarm(sender, EIFALMCD.JIGEMPTYTRAYID.ToString(), $"TRAY_OUTPUT_ST_Tray ID is Null or Empty", 7);

                                _EIFServer.SetVarStatusLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, $"HostTrouble=Tray ID is Null or Empty, Tray Step=[{trayStep}], Tray1=[{trayIDs[0]}], Tray2=[{trayIDs[1]}]");
                                return;
                            }
                        }
                    }

                    //Jig 적재 정보 보고 : 실 Tray 배출
                    string strBizName = string.Empty;
                    Exception ex = _EIFServer.MhsReport_StackStatus(SIMULATION_MODE, this.EQPID, this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, trayStep, trayIDs, eCarrierType.U, out strBizName); //$ 2023.01.10 : Common Method변경
                    if (ex != null)
                    {
                        HostBizAlarm(strBizName, this.EQPID, sender, ex, 4);
                        _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {System.Reflection.MethodBase.GetCurrentMethod().Name} -> Report Tray Stack Exception Occur!!");
                        return;
                    }

                    //Jig Selector Skip 여부
                    ReportJigSelectorSkip(trayIDs[0]);
                }

                //Jig Port 상태 변경
                string lowerTrayID = trayIDs.Count > 0 ? trayIDs[0] : string.Empty; //$ 2023.06.06 : Lower TrayID 정보 추출, Tray가 없을 경우 string.empty
                _EIFServer.MhsReport_PortStatus(SIMULATION_MODE, sender, this.EQPID, this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, eValue, lowerTrayID, eCarrierType.U); //$ 2023.01.10 : Common Method변경
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID, sender, ex);
            }
        }
        #endregion

        #region 실 Tray 적재기 //$ 2023.01.30 : 적재기 관련 내역 추가
        protected virtual void __S7_5_TRAY_TRNSINFO_02__I_B_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__I_B_TRAY_EXIST)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        protected virtual void __S7_5_TRAY_TRNSINFO_02__I_B_STACKED_TRAY_EXIST_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__I_B_STACKED_TRAY_EXIST)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        protected virtual void __S7_5_TRAY_TRNSINFO_02__I_B_TRIGGER_REPORT_OnBooleanChanged(CVariable sender, bool value)
        {
            string eventName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string txnID = _EIFServer.GenerateTransactionKey();

            try
            {
                string I_B_REQ = nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__I_B_TRIGGER_REPORT);
                string O_B_REP = nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF);
                string O_W_REP_ACK = nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_ACTION_CODE);

                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {eventName} -> [{I_B_REQ}] : {value}");

                if (!value)
                {
                    txnID = GetTxnID(eventName, txnID); //$ 2025.08.13 : Event Off 시점에 이전 ID 추출
                    SolaceLog(this.EQPID, txnID, 8, $"{GetDesc(I_B_REQ)} : Off"); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                    HostReply(this.EQPID, O_B_REP, value, O_W_REP_ACK, eConfirm.DEFAULT, txnID);

                    _EIFServer.SetVarStatusLog(this.Name, sender, $"HOST {eventName} -> [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF}");
                    return;
                }

                SetTxnID(eventName, txnID); //$ 2025.08.13 : Event On 시점에 ID 저장
                SolaceLog(this.EQPID, txnID, 1, $"{GetDesc(I_B_REQ)} : On");

                //$ 2024.02.14 : EIF 사전 검수용 Logic (NAK Test시 Nak 처리하고 Log를 남기고 리턴, Time Out은 그냥 리턴 하여 설비 Trouble 발생
                if (this.BASE.TESTMODE__V_IS_NAK_TEST || this.BASE.TESTMODE__V_IS_TIMEOUT_TEST)
                {
                    bool bOccur = RequestNGReply(eventName, this.EQPID, O_B_REP, O_W_REP_ACK);
                    if (bOccur) return;
                }

                string portID = this.EQP_INFO__V_W_ULD_OUTPUT_STATION_EQP_ID;
                string trayID = IsHR ? this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_TRAY_ID);
                string stackTrayID = IsHR ? this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_STACKED_TRAY_ID : _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_STACKED_TRAY_ID);

                SolaceLog(this.EQPID, txnID, 2, $"{GetDesc(nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_TRAY_ID))} : {trayID}, {GetDesc(nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__I_W_STACKED_TRAY_ID))} : {stackTrayID}");


                Int32 iRst = 0;
                string strBizName = "BR_MHS_RequestStackCommand";
                Exception bizEx = null;

                CBR_MHS_RequestStackCommand_IN inData = CBR_MHS_RequestStackCommand_IN.GetNew(this);
                CBR_MHS_RequestStackCommand_OUT outData = CBR_MHS_RequestStackCommand_OUT.GetNew(this);

                inData.IN_DATA_LENGTH = 1;

                //$ 2023.12.27 : UC2에서 MHS Biz에 USERID가 추가된 내역에 대해 추가 반영(기본 3개 항목중 USERID만추가 되었음.. 이상함.. 구조를 맞추기 위해 SRCTYPE 및 IFMODE는 반영 후 주석)
                //inData.IN_DATA[inData.IN_DATA_LENGTH - 1].SRCTYPE = SRCTYPE.EQUIPMENT;
                //inData.IN_DATA[inData.IN_DATA_LENGTH - 1].IFMODE = IFMODE.ONLINE;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].USERID = USERID.EIF;

                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].EQUIPMENT_ID = this.EQPID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].PORT_ID = portID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].DURABLE_ID = trayID;
                inData.IN_DATA[inData.IN_DATA_LENGTH - 1].DURABLE_STATUS_CODE = eCarrierType.U.ToString();

                inData.IN_STACKER_LIST_LENGTH = 0;
                if (!string.IsNullOrEmpty(stackTrayID))
                {
                    inData.IN_STACKER_LIST_LENGTH = 1;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].EQUIPMENT_ID = this.EQPID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].PORT_ID = portID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].DURABLE_ID = stackTrayID;
                    inData.IN_STACKER_LIST[inData.IN_STACKER_LIST_LENGTH - 1].DURABLE_STATUS_CODE = eCarrierType.U.ToString();
                }

                // 입력 데이터가 없으면.
                if (inData.IN_DATA.Count == 0)
                {
                    _EIFServer.SetVarStatusLog(this.Name, sender, $"LD Tray BCR Read Req No Input Data!! {this.EQPID} {trayID}");

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                // BizRule Call -> Current Inspector Tray#1 Arrived
                iRst = BizCall(strBizName, inData, outData, out bizEx, txnID);
                if (iRst != 0)
                {
                    _EIFServer.RegBizRuleException(SIMULATION_MODE, this.EQPID, strBizName, string.Empty, inData, bizEx);

                    _EIFServer.SetSolExcepLog(this.EQPID, txnID, bizEx.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)
                    _EIFServer.SetVarStatusLog(this.EQPID, sender, $"HOST {eventName} -> Report Tray Stack Command Exception Occur!!");

                    HostBizAlarm(strBizName, this.EQPID, sender, bizEx, 4);

                    //$ 2024.05.13 : 불필요한 설비 Timeout을 막기 위해 명시적으로 NAK Code 선언
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    return;
                }

                //BizRule 성공로그
                _EIFServer.SetBizRuleTableLog(strBizName, inData.Variable, outData.Variable);

                string sLog = string.Empty;
                if (IsRl)
                {
                    eStackCmd retVal = (ushort)eStackCmd.DEFAULT;
                    string resultCmd = outData.OUT_DATA[0].STACK_COMMAND;

                    //BizRule Call -> 적재기 Action Code (1.적재 2.배출 3.통과) , Tray Stack Count 요청.
                    if (resultCmd == STACKCMD.STACK || resultCmd == STACKCMD.STACKWAIT) // 적재
                        retVal = eStackCmd.STACK;
                    else
                        retVal = eStackCmd.REPLACE;

                    if (retVal != 0)
                    {
                        this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_STACK_COUNT = 2; //IsRl ? outData.OUTDATA[0].STACK_CNT : _EIFServer.GetSimValue(() => this.STACK_TRAY_INFO__O_W_LOADER_STACK_COUNT);
                        this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_WAIT_TIME = this.BASE.STACK_TRAY_INFO__V_W_LOADER_WAIT_TIME;
                        this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_BCR_READ_COUNT = this.BASE.STACK_TRAY_INFO__V_W_LOADER_BCR_READ_COUNT;

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, (eConfirm)retVal, txnID);

                        sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF} - {resultCmd}";
                        _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                    }
                    else
                    {
                        sLog = $"HOST NAK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF} - {resultCmd}";
                        _EIFServer.SetVarStatusLog(this.Name, sender, sLog);

                        HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, eConfirm.NAK, txnID);
                    }
                }
                else
                {
                    ushort retVal = _EIFServer.GetSimValue(() => this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_ACTION_CODE);
                    HostReply(this.EQPID, O_B_REP, true, O_W_REP_ACK, (eConfirm)retVal, txnID);

                    sLog = $"HOST ACK {eventName} [{O_B_REP}] : {this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF} - {retVal} : 1 - STACK, 2 - REPLACE";
                    _EIFServer.SetVarStatusLog(this.Name, sender, sLog);
                }
            }
            catch (Exception ex)
            {
                _EIFServer.SetErrorLog(sender, ex);
                _EIFServer.SetSolExcepLog(this.EQPID, txnID, ex.ToString()); //$ 2025.05.14 : Test용  전극/조립쪽과 같은 I/O 명으로 Log를 남기기)

                //JH 2024.07.15 Timeout 개선 NAK 코드
                this.BASE.S7_5_TRAY_TRNSINFO_02__O_W_ACTION_CODE = (ushort)eConfirm.NAK;
                this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_TRIGGER_REPORT_CONF = true;
            }
        }

        protected virtual void __S7_5_TRAY_TRNSINFO_02__O_B_BCR_RETRY_OnBooleanChanged(CVariable sender, bool value)
        {
            try
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"EQP {System.Reflection.MethodBase.GetCurrentMethod().Name} -> [{nameof(this.BASE.S7_5_TRAY_TRNSINFO_02__O_B_BCR_RETRY)}] : {value}");
            }
            catch (Exception ex)
            {
                _EIFServer.SetVarStatusLog(this.Name, sender, $"{System.Reflection.MethodBase.GetCurrentMethod().Name} - BCR Read Retry Host Trouble");
                _EIFServer.SetErrorLog(sender, ex);
            }
        }
        #endregion
    }
}