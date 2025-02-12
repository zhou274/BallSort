using System.Collections;
using System.Collections.Generic;
using Game;
using MyGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarkSDKSpace;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;

public class LevelCompletePanel : ShowHidable
{
    [SerializeField] private TextMeshProUGUI _toastTxt;
    [SerializeField]private List<string> _toasts = new List<string>();
    private StarkAdManager starkAdManager;

    public string clickid;

    protected override void OnShowCompleted()
    {
        ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
        base.OnShowCompleted();
        _toastTxt.text = _toasts.GetRandom();
        _toastTxt.gameObject.SetActive(true);

        AdsManager.ShowOrPassAdsIfCan();
    }


    public void OnClickContinue()
    {
        UIManager.Instance.LoadNextLevel();
    }
    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="errorCallBack"></param>
    /// <param name="closeCallBack"></param>
    public void ShowInterstitialAd(string adId, System.Action closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            var mInterstitialAd = starkAdManager.CreateInterstitialAd(adId, errorCallBack, closeCallBack);
            mInterstitialAd.Load();
            mInterstitialAd.Show();
        }
    }
}