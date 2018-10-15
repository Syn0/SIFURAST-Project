using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class wwwManager : MonoBehaviour
{
    public static wwwManager instance { get; private set; }
    //
    public InputField input_email;
    public InputField input_password;
    public Text txt_status;
    public Button btn_connect;
    //
    public string url_root = "https://legranre.ddns.net";
    public string url_Login;
    public string url_TestSession;
    public string url_GetToken;
    public string url_TestToken;
    public string url_Ping;
    public string url_SetUserStatus;
    public string url_GetFriendsListInfo;
    //
    public Antiforgery AntiforgeryToken;
    public LoginResult LIResult;
    public bool showHeaders;
    public Dictionary<string, string> CookieHashTable = new Dictionary<string, string>();
    //--------//
    public enum serverStatus { NotConnected,NotAvailable,Connected}
    public enum UserStatusCode { Undefined, Online, Offline, InRoom, Afk }
    public bool ServerAvailable { get; private set; }
    public serverStatus ServerStatus { get; private set; }
    //--------//
    void Start()
    {
        // TEST SERIALIZATE
        string test = "[\r\n  {\r\n    \"UserName\": \"llr.probox@gmail.com\",\r\n    \"Email\": \"llr.probox@gmail.com\",\r\n    \"Icon\": null,\r\n    \"LastTimeSeen\": \"0001-01-01T00:00:00\",\r\n    \"OnlineStatus_ID\": null,\r\n    \"OnlineStatus_Description\": null,\r\n    \"GamePoints\": null,\r\n    \"FullName\": null,\r\n    \"GameConnected\": null,\r\n    \"Country\": null,\r\n    \"Biographie\": null,\r\n    \"Location\": null\r\n  }\r\n]";
        DebugLog(JsonConvert.DeserializeObject<List<UnityUserInfo>>(FormatJson2(test)).Count.ToString());

        ServerAvailable = false;
        ServerStatus = serverStatus.NotConnected; ;
        instance = this;
        setupUrls();
        StartCoroutine(PingServer());
    }
    void Update()
    {
    }
    void setupUrls()
    {
        DebugLog(">> SETUP_URL");
        url_Login = url_root + "/Game/LogMe";
        url_TestSession = url_root + "/Game/TestSession";
        url_TestToken = url_root + "/Game/TestAntiForgeryToken";
        url_GetToken = url_root + "/Game/GetToken";
        url_Ping = url_root + "/Game/Ping";
        url_GetFriendsListInfo = url_root + "/Game/FriendsListInfo";
        url_SetUserStatus = url_root + "/Game/url_SetUserStatus";
    }
    public void Connect()
    {
        //PhotonNetwork.Server == ServerConnection.MasterServer
        if (!PhotonNetwork.connectedAndReady) { DebugLog("Log first to Photon"); return; }
        //btn_connect.interactable = false;
        StartCoroutine(GetAFT());
    }
    //--------//
    public Antiforgery ParseJsonAFT(string json)
    {
        string[] tmp = JsonConvert.DeserializeObject<AntiForgeryToken>(FormatJson(json)).__RequestVerificationToken.Split(':');
        return new Antiforgery() { cookieAFT = tmp[0], formAFT = tmp[1] }; 
    }
    public string FormatJson(string json)
    {
        return json.Trim('"').Replace("\\u0027", "\"");
    }
    public string FormatJson2(string json)
    {
        return json.Trim('"')
            .Replace("\\\"", "\"")
            .Replace("\\r\\n", "\r\n")
            ;//.Replace("\\u0027", "\"");
    }
    //--------//
    IEnumerator PingServer()
    {
        DebugLog(">> PING.SERVER");
        UnityWebRequest www = UnityWebRequest.Post(url_Ping, "");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); DebugLog("Server Offline"); ServerAvailable = false; yield break; }
        //
        ReadResponse(www, showHeaders);
        ServerAvailable = true;
        //
        DebugLog("Server Online");
    }
    //--------//
    IEnumerator GetAFT()
    {
        DebugLog(">> GET.ANTIFORGERYTOKEN");
        UnityWebRequest www;
        www = UnityWebRequest.Post(url_GetToken, "");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        //
        ReadResponse(www, showHeaders);
        AntiforgeryToken = ParseJsonAFT(www.downloadHandler.text);
        //
        DebugLog(">> GET.ANTIFORGERYTOKEN COMPLETE");
        StartCoroutine(TestAFT());
    }
    IEnumerator TestAFT()
    {
        DebugLog(">> TEST.ANTIFORGERYTOKEN");
        if (AntiforgeryToken==null) yield break;
        WWWForm form = new WWWForm();
        form.AddField("__RequestVerificationToken", AntiforgeryToken.formAFT);
        UnityWebRequest www = UnityWebRequest.Post(url_TestToken, form);
        www.SetRequestHeader("Cookie", "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + ";");
        DebugLog(">> TESTING Antiforgery ...");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); }
        else { ReadResponse(www, showHeaders); }
        //
        DebugLog(">> TEST.ANTIFORGERYTOKEN COMPLETE");
        StartCoroutine(Login());
    }
    IEnumerator Login()
    {
        DebugLog(">> START WEB FORMS");
        UnityWebRequest www;
        WWWForm form = new WWWForm();
        form.AddField("__RequestVerificationToken", AntiforgeryToken.formAFT);
        form.AddField("Password", input_password.text);
        input_password.text = "";
        form.AddField("Email", input_email.text);
        form.AddField("RememberMe", "true");
        //
        www = UnityWebRequest.Post(url_Login, form);
        www.SetRequestHeader("Cookie", "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + "; " + GetCookieString());
        DebugLog(">> SENDING login ...");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        ReadResponse(www, showHeaders);
        //
        string LIResult = FormatJson(www.downloadHandler.text);
        //
        bool loginOK = LIResult == "Success";
        if (!loginOK) { DebugLog(">> LOGIN FAILED ");  }
        else
        {
            DebugLog(">> LOGIN COMPLETE");
            ServerStatus = serverStatus.NotConnected;
            StartCoroutine(GetSessionAFT());
        }
    }
    //--------//
    IEnumerator GetSessionAFT()
    {
        DebugLog(">> GET.SESSION.ANTIFORGERYTOKEN");
        UnityWebRequest www;
        www = UnityWebRequest.Post(url_GetToken, "");
        www.SetRequestHeader("Cookie",GetCookieString());
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        //
        ReadResponse(www, showHeaders);
        AntiforgeryToken = ParseJsonAFT(www.downloadHandler.text);
        //
        DebugLog(">> GET.SESSION.ANTIFORGERYTOKEN COMPLETE");
        StartCoroutine(TestSession());
    }
    IEnumerator TestSession()
    {
        UnityWebRequest www;
        WWWForm form = new WWWForm();
        form.AddField("__RequestVerificationToken", AntiforgeryToken.formAFT);
        www = UnityWebRequest.Post(url_TestSession, form);
        www.SetRequestHeader("Cookie", "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + "; " + GetCookieString());
        DebugLog(">> TESTING login \nWith Cookie = " + "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + ";\n" + GetCookieString());
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        ReadResponse(www, showHeaders); 
        //
        DebugLog(">> TESTSESSION COMPLETE");
        txt_status.text = "Connexion avec succès !";


        new WebCredential(new PlayerCredential { UserEmail = input_email.text, UserId = "NA", UserName = input_email.text.Split('@')[0] },AntiforgeryToken);
        //
        StartCoroutine(SetUserStatus());
    }
    //--------//
    IEnumerator SetUserStatus()
    {
        UnityWebRequest www;
        WWWForm form = new WWWForm();
        form.AddField("__RequestVerificationToken", AntiforgeryToken.formAFT);
        form.AddField("Status", ((int)UserStatusCode.Online).ToString());
        www = UnityWebRequest.Post(url_SetUserStatus, form);
        www.SetRequestHeader("Cookie", "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + "; " + GetCookieString());
        DebugLog(">> SET USER STATUS : Online");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        ReadResponse(www, showHeaders);
        //
        DebugLog(">> Status is now Online");
        //
        StartCoroutine(GetFriendList());
    }
    IEnumerator GetFriendList()
    {
        UnityWebRequest www;
        WWWForm form = new WWWForm();
        form.AddField("__RequestVerificationToken", AntiforgeryToken.formAFT);
        www = UnityWebRequest.Post(url_GetFriendsListInfo, form);
        www.SetRequestHeader("Cookie", "__RequestVerificationToken=" + AntiforgeryToken.cookieAFT + "; " + GetCookieString());
        DebugLog(">> GetFriendList");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); yield break; }
        ReadResponse(www, showHeaders);
        List<UnityUserInfo> frndlst = JsonConvert.DeserializeObject<List<UnityUserInfo>>(FormatJson2(www.downloadHandler.text));
        //
        DebugLog(">> GetFriendList COMPLETE");
        //
        while (!PhotonNetwork.connected) new WaitForSecondsRealtime(2f);
        MNG_Multiplayer.instance.CreateRoom();
        StartCoroutine(MoveToGameScene());
    }
    //--------//
    IEnumerator MoveToGameScene()
    {
        // Temporary disable processing of futher network messages
        PhotonNetwork.isMessageQueueRunning = false;
        SceneManager.LoadScene("1-2d_level");//SceneManager.GetActiveScene().name); // custom method to load the new scene by name
        while (true/*newSceneDidNotFinishLoading*/)
        {
            yield return null;
        }
        PhotonNetwork.isMessageQueueRunning = true;
    }
    //
    void ReadResponse(UnityWebRequest www, bool logHeaders = false)
    {
        if (www.isNetworkError || www.isHttpError)
        {
            DebugLog(www.error);
        }
        else
        {
            // Print Body
            DebugLog("[WWW.BODY]\n" + www.downloadHandler.text);
            // Print Headers
            if (logHeaders)
            {
                StringBuilder sb = new StringBuilder();
                foreach (System.Collections.Generic.KeyValuePair<string, string> dict in www.GetResponseHeaders()) sb.Append("[HEAD] ").Append(dict.Key).Append(": \t[").Append(dict.Value).Append("]\n");
                DebugLog("[WWW.HEADER]\n" + sb.ToString());
            }
            if (www.GetResponseHeaders().ContainsKey("Set-Cookie"))
            {
                //print Set-Cookie
                List<string> cookies = www.GetResponseHeaders()["Set-Cookie"].Split(new char[] { ',', ';' }).Where(s => s.Contains('=')).ToList();

                string cookieNameTmp, cookieValueTmp;
                string[] cookieOptionsNames = new string[] { "HttpOnly", "expires", "path" };
                foreach (string cookie in cookies.Where(s => s.Contains('=') && !cookieOptionsNames.Contains(s.Trim())))
                {
                    cookieNameTmp = cookie.Split('=')[0].Trim();
                    cookieValueTmp = cookie.Split('=')[1];
                    if (!cookieOptionsNames.Contains(cookieNameTmp))
                    {
                        DebugLog("[COOKIE] " + cookieNameTmp + " = " + cookieValueTmp);
                        CookieHashTable[cookieNameTmp] = cookieValueTmp;
                    }
                }
            }

        }
    }
    public string GetCookieString()
    {
        //.AspNet.ApplicationCookie
        string res = "";
        foreach (string key in CookieHashTable.Keys)
        {
            res += key + "=" + CookieHashTable[key] + "; ";
        }
        return res;
    }
    IEnumerator WaitForRequest(WWW data)
    {
        yield return data; // Wait until the download is done
        if (data.error != null)
        {
            DebugLog("There was an error sending request: " + data.error);
        }
        else
        {
            DebugLog("WWW Request: " + data.text);
        }
    }
    void DebugLog(string str)
    {
        Debug.Log(str);
        txt_status.text = str;
        //go_text.text = go_text.text + "\n" + str;
    }

}

public class WebCredential
{
    public static PlayerCredential playerCredential { get; private set; }
    public static Antiforgery antiforgery { get; private set; }
    public WebCredential(PlayerCredential _playerCredential, Antiforgery _antiforgery) { playerCredential = _playerCredential;antiforgery = _antiforgery; }
}

public struct PlayerCredential
{
    public string UserName;
    public string UserEmail;
    public string UserId;
}

public class AntiForgeryToken
{
    public string __RequestVerificationToken { get; set; }
}
public class Antiforgery
{
    public string formAFT;
    public string cookieAFT;
}
public class LoginResult
{
    public string Result;
    public string session_id;
}

public class UnityUserInfo
{
    /*
     "[\r\n  {\r\n    \"UserName\": \"llr.probox@gmail.com\",\r\n    \"Email\": \"llr.probox@gmail.com\",\r\n    \"Icon\": null,\r\n    \"LastTimeSeen\": \"0001-01-01T00:00:00\",\r\n    \"OnlineStatus_ID\": null,\r\n    \"OnlineStatus_Description\": null,\r\n    \"GamePoints\": null,\r\n    \"FullName\": null,\r\n    \"GameConnected\": null,\r\n    \"Country\": null,\r\n    \"Biographie\": null,\r\n    \"Location\": null\r\n  }\r\n]"
     
      
     "[
         {
             \"UserName\": \"llr.probox@gmail.com\",
             \"Email\": \"llr.probox@gmail.com\",
             \"Icon\": null,
             \"LastTimeSeen\": \"0001-01-01T00:00:00\",
             \"OnlineStatus_ID\": null,
             \"OnlineStatus_Description\": null,
             \"GamePoints\": null,
             \"FullName\": null,
             \"GameConnected\": null,
             \"Country\": null,
             \"Biographie\": null,
             \"Location\": null
         }
     ]"


         */
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Icon { get; set; }
    public DateTime LastTimeSeen { get; set; }
    public int? OnlineStatus_ID { get; set; }
    public string OnlineStatus_Description { get; set; }
    public int? GamePoints { get; set; }
    public string FullName { get; set; }
    public int? GameConnected { get; set; }
    public string Country { get; set; }
    public string Biographie { get; set; }
    public string Location { get; set; }
}

/* -------- EXAMPLES//
var setting = new JsonSerializerSettings();
setting.Formatting = Formatting.Indented;
setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

var path = Path.Combine(Application.dataPath, "hi.json");

// write
var accountsFromCode = new List<Account> { accountJames, accountOnion };
var json = JsonConvert.SerializeObject(accountsFromCode, setting);
File.WriteAllText(path, json);

// read
var fileContent = File.ReadAllText(path);
var accountsFromFile = JsonConvert.DeserializeObject<List<Account>>(fileContent);
var reSerializedJson = JsonConvert.SerializeObject(accountsFromFile, setting);

print(reSerializedJson);
print("json == reSerializedJson is" + (json == reSerializedJson));
*/
