/* SugarCRM SOAP API Wrapper
 * 
 * Date:        February 15, 2009
 * 
 * Author:      Angel Magaña
 * 
 * Contact:     cheleguanaco[at]cheleguanaco.com
 *              cheleguanaco.com
 *     
 *              
 * Description: Simplifies communication with SOAP API.
 *              Translates more common .NET calls to "Sugar-ese."
 *              
 *              Copyright 2009 - 2011 Angel Magaña
 *              
 *              Licensed under the Apache License, Version 2.0 (the "License"); 
 *              you may not use this file except in compliance with the License. 
 *              
 *              You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
 *              
 *              Unless required by applicable law or agreed to in writing, software distributed under 
 *              the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF 
 *              ANY KIND, either express or implied. See the License for the specific language governing 
 *              permissions and limitations under the License. 
 * 
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Web.Services;
using System.Net;

namespace CandyWrapper
{
    public class CandyWrapper
    {
        private SugarCRM.sugarsoap oSugarCRM = new SugarCRM.sugarsoap();

        public string doLogin(string sUser, string sPass, string sVersion, string sAppName)
        {
            SugarCRM.user_auth oSugarUserAuth = new SugarCRM.user_auth();
            SugarCRM.set_entry_result oSugarSetEntryRes = new SugarCRM.set_entry_result();
            string sPassHash = this.getMD5Hash(sPass);
            string sResult = string.Empty;

            oSugarUserAuth.user_name = sUser;
            oSugarUserAuth.password = sPassHash;
            oSugarUserAuth.version = sVersion;

            try
            {
                oSugarSetEntryRes = oSugarCRM.login(oSugarUserAuth, sAppName);
                //Check if error occurred
                sResult = oSugarSetEntryRes.id;
                if (oSugarSetEntryRes.error.number != "0")
                {
                    sResult = oSugarSetEntryRes.error.number;
                }
            }
            catch (Exception ex)
            {
                sResult = "05";
            }
        
            return sResult;

        }

        public string doLogout(string sSession)
        {
            SugarCRM.error_value oSugarErrVal = oSugarCRM.logout(sSession);

            //Check if error occurred
            string sResult = "Logout Successful!";
            if (oSugarErrVal.number != "0")
            {
                sResult = "Logout Error: ";
                sResult += oSugarErrVal.number + "\n";
                sResult += oSugarErrVal.description;
            }

            return sResult;
        }

        public int doSeamlessLogin(string sSession)
        {
            int sMSID = oSugarCRM.seamless_login(sSession);
            
            return sMSID;
        }

        public string[,] doGetEntryList(string sSession, string sModule, string sQuery, string sOrder, int iOffset, string[] sFields, int iLimit, int iDel)
        {
            sModule = this.doConvertToProper(sModule);
            string[,] sResults = null;

            try
            {
                SugarCRM.get_entry_list_result oSugarGetListRes = oSugarCRM.get_entry_list(sSession, sModule, sQuery, sOrder, iOffset, sFields, iLimit, iDel);
                SugarCRM.error_value oSugarErrVal = oSugarGetListRes.error;

                if (oSugarErrVal.number != "0")
                {
                    string[,] saErrResults = new string[1, 1];
                    saErrResults[0, 0] = oSugarErrVal.number;
                    saErrResults[0, 1] = oSugarErrVal.description;
                    return saErrResults;
                }
                
                SugarCRM.entry_value[] oSugarListVals = oSugarGetListRes.entry_list;

                int iRows = oSugarGetListRes.result_count;
                int iColumns = sFields.Length + 1;  //Additional column added to accommodate ID value

                sResults = new string[iRows, iColumns];

                //Iterate through each row to process results
                int iRecords = 0;
                foreach (SugarCRM.entry_value oSugarEntryVal in oSugarListVals)
                {
                    int iCounter = 1;
                    SugarCRM.name_value[] oSugarNV = oSugarEntryVal.name_value_list;

                    //First column on each row is always the ID value
                    sResults[iRecords, 0] = oSugarEntryVal.id;

                    //Iterate through the rest of the columns before moving to next record
                    foreach (SugarCRM.name_value oSugarVal in oSugarNV)
                    {
                        sResults[iRecords, iCounter] = oSugarVal.value;
                        iCounter++;
                    }

                    iRecords++;
                }

            }
            catch (Exception ex)
            {
                sResults = new string[1, 1];
                sResults[0, 0] = "0";
            }

            return sResults;
        }

        public string[] doGetEntry(string sSession, string sModule, string sID, string[] sFields)
        {
            sModule = this.doConvertToProper(sModule);
            SugarCRM.get_entry_result oSugarGetEntryRes = oSugarCRM.get_entry(sSession, sModule, sID, sFields);

            int iColumns = oSugarGetEntryRes.field_list.Length;
            string[] sResults = new string[iColumns];

            //Iterate through all the fields
            SugarCRM.entry_value[] oSugarEntryVals = oSugarGetEntryRes.entry_list;
            SugarCRM.name_value[] oSugarNV = oSugarEntryVals[0].name_value_list;

            int iCounter = 0;
            foreach (SugarCRM.name_value oSugarVal in oSugarNV)
            {
                sResults[iCounter] = oSugarVal.value;
                iCounter++;
            }

            return sResults;
        }

        public string doGetUserID(string sSession)
        {
            string sResult = oSugarCRM.get_user_id(sSession);

            return sResult;
        }

        public string doGetUserTeamID(string sSession)
        {
            string sResult = oSugarCRM.get_user_team_id(sSession);

            return sResult;
        }

        public string doSetEntry(string sSession, string sModule, string[] saNames, string[] saValues)
        {
            int iNVLength = saNames.Length;
            int iCounter = 0;
            string sResults = string.Empty;
            sModule = this.doConvertToProper(sModule);

            //Convert array to Sugar NV pairing            
            SugarCRM.name_value[] oSugarNVList = new SugarCRM.name_value[iNVLength];

            while (iCounter < iNVLength)
            {
                oSugarNVList[iCounter] = new SugarCRM.name_value();
                oSugarNVList[iCounter].name = saNames[iCounter];
                oSugarNVList[iCounter].value = saValues[iCounter];
                iCounter++;
            }

            SugarCRM.set_entry_result oSugarSetEntryRes = null;

            try
            {
                oSugarSetEntryRes = oSugarCRM.set_entry(sSession, sModule, oSugarNVList);
            }
            catch (Exception ex)
            {
                sResults = "doSetEntry Error: " + ex.Message;
                return sResults;
            }

            SugarCRM.error_value oSugarErrVal = oSugarSetEntryRes.error;

            if (oSugarErrVal.number != "0")
            {
                sResults = "Error Creating Record: " + oSugarErrVal.number + "\r\n" + oSugarErrVal.description;
            }
            else
            {
                sResults = oSugarSetEntryRes.id;
            }

            return sResults;
         
        }

        public string[] doGetAvailableModules(string sSession)
        {
            SugarCRM.module_list oSugarModList = oSugarCRM.get_available_modules(sSession);
            SugarCRM.error_value oSugarErrVal = oSugarModList.error;
            int iLength = 1;
            if (oSugarModList.modules.Length > 0)
            {
                iLength = oSugarModList.modules.Length;
            }
            string[] saResults = new string[iLength];
            
            if (oSugarErrVal.number == "0")
            {
                int iTemp = 0;
                foreach (string sModule in oSugarModList.modules)
                {
                    saResults[iTemp] = sModule;
                    iTemp++;
                }
            }
            else
            {
                saResults[0] = oSugarErrVal.number;
            }

            return saResults;
        }

        public string[] doGetModuleFields(string sSession, string sModule)
        {
            sModule = this.doConvertToProper(sModule);
            SugarCRM.module_fields oSugarModFields = oSugarCRM.get_module_fields(sSession, sModule);
            string[] saFields = new string[oSugarModFields.module_fields1.Length];
            SugarCRM.field[] oSugarFields = oSugarModFields.module_fields1;

            int iTemp = 0;
            foreach (SugarCRM.field oSugarField in oSugarFields)
            {
                saFields[iTemp] = oSugarField.name;
                iTemp++;
            }

            return saFields;

            //TODO: Need to put in result success/failure for return
        }

        public string[] doGetRelationships(string sSession, string sModule, string sID, string sRelMod, string sRelModQuery, int iDeleted)
        {
            sModule = this.doConvertToProper(sModule);
            SugarCRM.get_relationships_result oSugarRelRes = oSugarCRM.get_relationships(sSession, sModule, sID, sRelMod, sRelModQuery, iDeleted);
            SugarCRM.error_value oSugarErr = oSugarRelRes.error;
            
            if (oSugarErr.number != "0")
            {
                string[] sErrResults = new string[] {"Error: " + oSugarErr.number + "\r\n" + oSugarErr.description};
                return sErrResults;
            }

            SugarCRM.id_mod[] oSugarIDMods = oSugarRelRes.ids;

            string[] sResults = new string[oSugarIDMods.Length];      
            int iCounter = 0;

            foreach (SugarCRM.id_mod oSugarIDMod in oSugarIDMods)
            {
                sResults[iCounter] = oSugarIDMod.id;
                iCounter++;
            }

            return sResults;
        }

        public string doRelateRecord(string sSession, string sParent, string sParentID, string sChild, string sChildID)
        {
            string sResults = string.Empty;
            sParent = this.doConvertToProper(sParent);
            sChild = this.doConvertToProper(sChild);
            SugarCRM.set_relationship_value oSugarSetRelVal = new SugarCRM.set_relationship_value();
            oSugarSetRelVal.module1 = sParent;
            oSugarSetRelVal.module1_id = sParentID;
            oSugarSetRelVal.module2 = sChild;
            oSugarSetRelVal.module2_id = sChildID;

            SugarCRM.error_value oSugarErrVal = oSugarCRM.set_relationship(sSession, oSugarSetRelVal);
            //TODO: Error checking/handling
            sResults = oSugarErrVal.number + ":" + oSugarErrVal.description;

            return sResults;

        }

        public string doCreateAccount(string sUser, string sPass, string sName, string sPhone, string sURL)
        {
            string sResult = oSugarCRM.create_account(sUser, sPass, sName, sPhone, sURL);
            return sResult;
        }

        public string doCreateContact(string sUser, string sPass, string sFirst, string sLast, string sEmail)
        {
            string sResult = oSugarCRM.create_contact(sUser, sPass, sFirst, sLast, sEmail);
            return sResult;
        }

        public string doCreateLead(string sUser, string sPass, string sFirst, string sLast, string sEmail)
        {
            string sResult = oSugarCRM.create_lead(sUser, sPass, sFirst, sLast, sEmail);
            return sResult;
        }

        public string doCreateCase(string sUser, string sPass, string sName)
        {
            string sResult = oSugarCRM.create_case(sUser, sPass, sName);
            return sResult;
        }

        public string doCreateOpportunity(string sUser, string sPass, string sName, string sAmount)
        {
            string sResult = oSugarCRM.create_opportunity(sUser, sPass, sName, sAmount);
            return sResult;
        }

        public void doContactByEmail(string sUser, string sPass, string sEmail)
        {
            //TODO: NOT FINISHED
            SugarCRM.contact_detail[] oSugarContDetails = oSugarCRM.contact_by_email(sUser, sPass, sEmail);

            foreach (SugarCRM.contact_detail oSugarContDetail in oSugarContDetails)
            {
                Console.WriteLine(oSugarContDetail.association);
                Console.WriteLine(oSugarContDetail.email_address);
                Console.WriteLine(oSugarContDetail.id);
                Console.WriteLine(oSugarContDetail.msi_id);
                Console.WriteLine(oSugarContDetail.name1);
                Console.WriteLine(oSugarContDetail.name2);
                Console.WriteLine(oSugarContDetail.type);
            }

        }

        public string doCreateSession(string sUser, string sPass)
        {
            string sResult = oSugarCRM.create_session(sUser, sPass);

            return sResult;
        }

        public string doEndSession(string sUser)
        {
            string sResult = oSugarCRM.end_session(sUser);

            return sResult;
        }

        public string doGetGMTTime()
        {
            string sResult = oSugarCRM.get_gmt_time();

            return sResult;
        }

        public string doGetServerTime()
        {
            string sResult = oSugarCRM.get_server_time();

            return sResult;
        }

        public string doGetServerVersion()
        {
            string sResult = oSugarCRM.get_server_version();

            return sResult;
        }

        public string doGetSugarFlavor()
        {
            string sResult = oSugarCRM.get_sugar_flavor();

            return sResult;
        }

        public int doIsLoopback()
        {
            int iResult = oSugarCRM.is_loopback();

            return iResult;
        }

        public int doIsUserAdmin(string sSession)
        {
            int iResult = oSugarCRM.is_user_admin(sSession);

            return iResult;
        }

        public string doRelateNoteToModule(string sSession, string sNoteID, string sModule, string sModuleID)
        {
            SugarCRM.error_value oSugarError = oSugarCRM.relate_note_to_module(sSession, sNoteID, sModule, sModuleID);

            string sErrNum = oSugarError.number;
            string sErrDesc = oSugarError.description;

            string sResult = "Error: " + sErrNum + "\n";
            sResult += sErrDesc;

            return sResult;
        }

        public string[,] doSearch(string sUser, string sPass, string sValue)
        {
            SugarCRM.contact_detail[] oSugarContDetails = oSugarCRM.search(sUser, sPass, sValue);
            string[,] saResults = new string[oSugarContDetails.Length, 7];
            int iCounter = 0;

            foreach (SugarCRM.contact_detail oSugarContDetail in oSugarContDetails)
            {
                saResults[iCounter, 0] = oSugarContDetail.association;
                saResults[iCounter, 1] = oSugarContDetail.email_address;
                saResults[iCounter, 2] = oSugarContDetail.id;
                saResults[iCounter, 3] = oSugarContDetail.msi_id;
                saResults[iCounter, 4] = oSugarContDetail.name1;
                saResults[iCounter, 5] = oSugarContDetail.name2;
                saResults[iCounter, 6] = oSugarContDetail.type;

                iCounter++;
            }

            return saResults;
        }

        public string[,] doSearchByModule(string sUser, string sPass, string sValue, string[] saModules, int iOffset, int iLimit)
        {
            SugarCRM.get_entry_list_result oSugarGetEntryListRes = oSugarCRM.search_by_module(sUser, sPass, sValue, saModules, iOffset, iLimit);
            SugarCRM.entry_value[] oSugarEntryVals = oSugarGetEntryListRes.entry_list;

            string[,] saResults = new string[oSugarEntryVals.Length, 2];

            Console.WriteLine("ALF: " + oSugarEntryVals.LongLength);

            foreach (SugarCRM.entry_value oSugarEntryVal in oSugarEntryVals)
            {
                SugarCRM.name_value[] oSugarNameVals = oSugarEntryVal.name_value_list;
                Console.WriteLine("NVLen:" + oSugarNameVals.Length);

                int iCounter = 0;
                foreach (SugarCRM.name_value oSugarNameVal in oSugarNameVals)
                {
                    //saResults[iCounter, 0] = oSugarNameVal.name;
                    //saResults[iCounter, 1] = oSugarNameVal.value;
                    Console.WriteLine(oSugarNameVal.name);
                    Console.WriteLine(oSugarNameVal.value);
                    Console.WriteLine(oSugarEntryVal.module_name);
                    iCounter++;
                }
            }

            return saResults;
        }

        public void doSetNoteAttachment(string sSession, string sFilePathName, string sFileName, string sID)
        {
            SugarCRM.note_attachment oSugarNoteAttachment = new SugarCRM.note_attachment();
            oSugarNoteAttachment.file = sFilePathName;
            oSugarNoteAttachment.filename = sFileName;
            oSugarNoteAttachment.id = sID;
            SugarCRM.set_entry_result oSugarSetEntryResult = oSugarCRM.set_note_attachment(sSession, oSugarNoteAttachment);
        }

        public string[] doGetNoteAttachment(string sSession, string sID)
        {
            SugarCRM.return_note_attachment oSugarReturnNoteAttach = oSugarCRM.get_note_attachment(sSession, sID);
            SugarCRM.note_attachment oSugarNoteAttach = oSugarReturnNoteAttach.note_attachment;

            string[] saResults = new string[2];
            saResults[0] = oSugarNoteAttach.file;
            saResults[1] = oSugarNoteAttach.filename;

            return saResults;
        }

        //Utils section
        public string getMD5Hash(string sPass)
        {
            MD5 md5Temp = new MD5CryptoServiceProvider();
            Byte[] byteData = md5Temp.ComputeHash(Encoding.Default.GetBytes(sPass));
            StringBuilder sbTemp = new StringBuilder();
            int iCounter = 0;

            while (iCounter < byteData.Length)
            {
                sbTemp.Append(byteData[iCounter].ToString("x2"));
                iCounter++;
            }

            return sbTemp.ToString();
        }

        public int setURL(string sURL)
        {
            HttpWebRequest oWebReq = (HttpWebRequest) WebRequest.Create(sURL);
            int iResult = 0;

            try
            {
                HttpWebResponse oWebRes = (HttpWebResponse)oWebReq.GetResponse();
                oSugarCRM.Url = sURL;
                iResult = 1;
            }
            catch (Exception ex)
            {
                iResult = 0;
            }

            return iResult;
        }

        public int setURL(string sURL, string sUser, string sPass, string sType)
        {
            NetworkCredential netCreds = new NetworkCredential(sUser, sPass);

            CredentialCache credCache = new CredentialCache();
            credCache.Add(new Uri(sURL), sType, netCreds);

            HttpWebRequest oWebReq = (HttpWebRequest)WebRequest.Create(sURL);
            oWebReq.Credentials = credCache;
            int iResult = 0;

            try
            {
                HttpWebResponse oWebRes = (HttpWebResponse)oWebReq.GetResponse();
                oSugarCRM.Url = sURL;
                iResult = 1;
            }
            catch (Exception ex)
            {
                iResult = 0;
            }

            return iResult;
        }

        private string doConvertToProper(string sValue)
        {
            CultureInfo oCulture = Thread.CurrentThread.CurrentCulture;
            TextInfo oTextInfo = oCulture.TextInfo;

            string sReturn = oTextInfo.ToTitleCase(sValue);
            return sReturn;

        }

        public void doSetCredentials(string sUser, string sPass, string sURL, string sType)
        {
            NetworkCredential netCreds = new NetworkCredential(sUser, sPass);

            CredentialCache credCache = new CredentialCache();
            credCache.Add(new Uri(sURL), sType, netCreds);
            oSugarCRM.Credentials = credCache;

        }
    }
}
