using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameCreator.Variables;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatScript : MonoBehaviour
{
    public static ChatScript Instance;
    private void Awake()
    {
        Instance = this;
    }

    //API key
    [SerializeField] private string m_OpenAI_Key = "填写你的Key";
    //聊天UI层
    [SerializeField] private GameObject m_ChatPanel;
    //输入的信息
    [SerializeField] private InputField m_InputWord;
    //返回的信息
    [SerializeField] private Text m_TextBack;
    //播放设置
    [SerializeField] private Toggle m_PlayToggle;

    [SerializeField] private string m_lan = "使用中文回答";
    /// <summary>
    /// 百度语音识别
    /// </summary>
    [SerializeField] private BaiduSpeechSample m_SpeechSample;

    //gpt-3.5-turbo
    [SerializeField] public GptTurboScript m_GptTurboScript;

    private void OnEnable() {
        m_InputWord.enabled = true;
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    //发送信息
    public void SendData()
    {
        if (m_InputWord.text.Equals(""))
            return;

        m_InputWord.enabled = false;
        //记录聊天
        m_ChatHistory.Add(m_InputWord.text);

        string _msg = m_lan + " " + m_InputWord.text;
        //string _msg =m_lan + " " + m_InputWord.text;
        //发送数据
        //StartCoroutine (GetPostData (_msg,CallBack));
        StartCoroutine(m_GptTurboScript.GetPostData(_msg, m_OpenAI_Key, SpeechCallBack));

        m_InputWord.text = "";
        m_TextBack.text = "...";


    }
    //发送信息
    public void SendData(string _data)
    {
        //记录聊天
        m_ChatHistory.Add(_data);

        string _msg = m_lan + " " + _data;
        //string _msg =m_lan + " " + m_InputWord.text;
        //发送数据
        //StartCoroutine (GetPostData (_msg,CallBack));
        StartCoroutine(m_GptTurboScript.GetPostData(_msg, m_OpenAI_Key, SpeechCallBack));

        //m_InputWord.text = "";
        m_TextBack.text = "...";
    }

    //AI回复的信息
    private void SpeechCallBack(string _callback)
    {
        _callback = _callback.Trim();
        m_TextBack.text = "";

        //开始逐个显示返回的文本
        m_WriteState = true;
        //记录聊天
        m_ChatHistory.Add(_callback);

        if (m_PlayToggle.isOn)
        {
            StartCoroutine(Speek(_callback, TextCallBack));
        }
        else
        {
            StartCoroutine(SetTextPerWord(_callback));
        }

    }

    //AI回复的信息
    private void TextCallBack(string _callback)
    {
        StartCoroutine(SetTextPerWord(_callback));
    }


    private IEnumerator Speek(string _msg, System.Action<string> _callback)
    {
        yield return new WaitForEndOfFrame();
        //播放合成并播放音频
        m_SpeechSample.Speek(_msg, _callback);
    }

    #region 文字逐个显示
    //逐字显示的时间间隔
    [SerializeField] private float m_WordWaitTime = 0.2f;
    //是否显示完成
    [SerializeField] private bool m_WriteState = false;
    private IEnumerator SetTextPerWord(string _msg)
    {
        int currentPos = 0;
        while (m_WriteState)
        {
            yield return new WaitForSeconds(m_WordWaitTime);
            currentPos++;
            //更新显示的内容
            m_TextBack.text = _msg.Substring(0, currentPos);

            m_WriteState = currentPos < _msg.Length;
        }
        m_InputWord.enabled = true;
    }

    #endregion


    #region 聊天记录
    //保存聊天记录
    [SerializeField] private List<string> m_ChatHistory;
    //缓存已创建的聊天气泡
    [SerializeField] private List<GameObject> m_TempChatBox;
    //聊天记录显示层
    [SerializeField] private GameObject m_HistoryPanel;
    //聊天文本放置的层
    [SerializeField] private RectTransform m_rootTrans;
    //发送聊天气泡
    [SerializeField] private ChatPrefab m_PostChatPrefab;
    //回复的聊天气泡
    [SerializeField] private ChatPrefab m_RobotChatPrefab;
    //滚动条
    [SerializeField] private ScrollRect m_ScroTectObject;
    //获取聊天记录
    public void OpenAndGetHistory()
    {
        m_ChatPanel.SetActive(false);
        m_HistoryPanel.SetActive(true);

        ClearChatBox();
        StartCoroutine(GetHistoryChatInfo());
    }
    //返回
    public void BackChatMode()
    {
        m_ChatPanel.SetActive(true);
        m_HistoryPanel.SetActive(false);
    }

    //清空已创建的对话框
    private void ClearChatBox()
    {
        while (m_TempChatBox.Count != 0)
        {
            if (m_TempChatBox[0])
            {
                Destroy(m_TempChatBox[0].gameObject);
                m_TempChatBox.RemoveAt(0);
            }
        }
        m_TempChatBox.Clear();
    }

    //获取聊天记录列表
    private IEnumerator GetHistoryChatInfo()
    {

        yield return new WaitForEndOfFrame();

        for (int i = 0; i < m_ChatHistory.Count; i++)
        {
            if (i % 2 == 0)
            {
                ChatPrefab _sendChat = Instantiate(m_PostChatPrefab, m_rootTrans.transform);
                _sendChat.SetText(m_ChatHistory[i]);
                m_TempChatBox.Add(_sendChat.gameObject);
                continue;
            }

            ChatPrefab _reChat = Instantiate(m_RobotChatPrefab, m_rootTrans.transform);
            _reChat.SetText(m_ChatHistory[i]);
            m_TempChatBox.Add(_reChat.gameObject);
        }

        //重新计算容器尺寸
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_rootTrans);
        StartCoroutine(TurnToLastLine());
    }

    private IEnumerator TurnToLastLine()
    {
        yield return new WaitForEndOfFrame();
        //滚动到最近的消息
        m_ScroTectObject.verticalNormalizedPosition = 0;
    }


    #endregion


}
