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
using UnityEngine.UI;

public class wwwManager : MonoBehaviour
{
    public InputField input_email;
    public InputField input_password;
    public Text txt_status;
    public Button btn_connect;
    //
    public string url_base = "https://legranre.ddns.net";
    public string url_Login;
    public string url_TestSession;
    public string url_GetToken;
    public string url_TestToken;
    public string url_Ping;
    public string url_GetFriendsListInfo;
    public Antiforgery AntiforgeryToken;
    public LoginResult LIResult;
    public bool showHeaders;
    public Dictionary<string, string> CookieHashTable = new Dictionary<string, string>();
    //--------//
    void Start()
    {
        setupUrls();
    }
    void Update()
    {
    }
    void setupUrls()
    {
        DebugLog(">> SETUP_URL");
        url_Login = url_base + "/Game/LogMe";
        url_TestSession = url_base + "/Game/TestSession";
        url_TestToken = url_base + "/Game/TestAntiForgeryToken";
        url_GetToken = url_base + "/Game/GetToken";
        url_Ping = url_base + "/Game/Ping";
        url_GetFriendsListInfo = url_base + "/Game/GetFriendsListInfo";
    }

    public void Connect()
    {
        //btn_connect.interactable = false;
        DebugLog("### START : " + DateTime.Now);
        StartCoroutine(PingServer());
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
    //--------//
    IEnumerator PingServer()
    {
        DebugLog(">> PING.SERVER");
        UnityWebRequest www = UnityWebRequest.Post(url_Ping, "");
        yield return www.SendWebRequest();
        //
        if (www.isNetworkError || www.isHttpError) { DebugLog(www.error); DebugLog(">> ! AUNCUNE REPONSE DU SERVEUR !"); yield break; }
        //
        ReadResponse(www, showHeaders);
        //
        DebugLog(">> PING.SERVER : PONGED");
        StartCoroutine(GetAFT());
    }

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
        LIResult = JsonConvert.DeserializeObject<LoginResult>(FormatJson(www.downloadHandler.text));
        //
        bool loginOK = LIResult.Result == "Success";
        if (!loginOK) { DebugLog(">> LOGIN FAILED "); DebugLog("### END " + DateTime.Now); }
        else
        {
            DebugLog(">> LOGIN COMPLETE");
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
        List<FriendInfo> frndlst = JsonConvert.DeserializeObject<List<FriendInfo>>(FormatJson(www.downloadHandler.text));
        //
        DebugLog(">> GetFriendList COMPLETE");

    }
    //--------//
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




    public static string HashPassword(string password)
    {
        byte[] salt;
        byte[] buffer2;
        if (password == null)
        {
            throw new ArgumentNullException("password is null");
        }
        using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
        {
            salt = bytes.Salt;
            buffer2 = bytes.GetBytes(0x20);
        }
        byte[] dst = new byte[0x31];
        Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
        Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
        return Convert.ToBase64String(dst);
    }

    //-------- EXAMPLES//
    /*
    <form action="/Account/Login?ReturnUrl=%2FGame%2FTestIsLogged2"
    class="form-horizontal" method="post" role="form"><input
    name="__RequestVerificationToken" type="hidden" 
    value="
    tj1K-CzfGETdzAsuniwpODO_vy-3AELwIx5POgTCwXXTRkTBhZDJ7jHNP0yCbaCIpFoKB3AU0Tun5AyxlLX4WjQHN2OhEDUv4TWITOIpgJlqpKRH2JQpTp78W-ZLIZcZIZaMfar_o8qURBj14FRrWw2
    9LQ9x_YUTShaqXg2mm9FMj5nYSjWwpG3O9XI3Samg-v2yMlhhwRdFHVyWboNP8w5R_qiAWl7kUkaHoH8V56kkPgHS6uE_tcIVNQy5WOt77E1:am58EiP9OWtvOlxiwDmcaerbnn2-OhahPLgj8Kdh3VU4uSwcAqT_PEe1_gksgw9JGPgIod_0qXE9QzbGQJEHnY6-_NTEGLhJhrzlZWA9rLs1


    /*
    public WWW POST_JSON()
    {
        UnityEngine.WWW www;
        Hashtable postHeader = new Hashtable();
        postHeader.Add("Content-Type", "application/json");
        // convert json string to byte
        var formData = System.Text.Encoding.UTF8.GetBytes(jsonStr);
        www = new WWW(POSTAddUserURL, formData, postHeader);
        DebugLog(">> SENDED");
        StartCoroutine(WaitForRequest(www));
        return www;
    }
    */
    /*void test()
    {
        try
        {
            string url_registerEvent = "http://demo....?parameter1=" parameter1value"&parameter2="parameter2value;
            WebRequest req = WebRequest.Create(url_registerEvent);
            req.ContentType = "application/json";
            req.Method = "SET";
            //req.Credentials = new NetworkCredential ("connect10@gmail.com", "connect10api");
            HttpWebResponse resp = req.GetResponse() as HttpWebResponse;
            var encoding = resp.CharacterSet == ""
                        ? Encoding.UTF8
                        : Encoding.GetEncoding(resp.CharacterSet);
            using (var stream = resp.GetResponseStream())
            {
                var reader = new StreamReader(stream, encoding);
                var responseString = reader.ReadToEnd();

                Debug.Log("Result :" + responseString);
                //JObject json = JObject.Parse(str);
            }
        }
        catch (Exception e)
        {
            Debug.Log("ERROR : " + e.Message);
        }
    }
    */
}
//-------- EXAMPLES//
/*
public class UsingJsonDotNetInUnity : MonoBehaviour
{
    private void Awake()
    {
        var accountJames = new Account
        {
            Email = "james@example.com",
            Active = true,
            CreatedDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Roles = new List<string>
            {
                "User",
                "Admin"
            },
            Ve = new Vector3(10, 3, 1),
            StrVector3Dictionary = new Dictionary<string, Vector3>
            {
                {"start", new Vector3(0, 0, 1)},
                {"end", new Vector3(9, 0, 1)}
            }
        };


        var accountOnion = new Account
        {
            Email = "onion@example.co.uk",
            Active = true,
            CreatedDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Roles = new List<string>
            {
                "User",
                "Admin"
            },
            Ve = new Vector3(0, 3, 1),
            StrVector3Dictionary = new Dictionary<string, Vector3>
            {
                {"vr", new Vector3(0, 0, 1)},
                {"pc", new Vector3(9, 9, 1)}
            }
        };


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
    }

    public class Account
    {
        public string Email { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public IList<string> Roles { get; set; }
        public Vector3 Ve { get; set; }
        public Dictionary<string, Vector3> StrVector3Dictionary { get; set; }
    }

}
    */
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
    public string UserName { get; set; }
    public string Icon { get; set; }
    public bool IsActive { get; set; }
    public string LastTimeSeen { get; set; }
    public int OnlineStatus_ID { get; set; }
    public string OnlineStatus_Description { get; set; }
    public int? GamePoints { get; set; }
    public string FullName { get; set; }
    public int GameConnected { get; set; }
    public string Country { get; set; }
    public string Biographie { get; set; }
    public string Location { get; set; }
}