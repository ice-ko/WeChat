using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using log4net;
using log4net.Repository;
using System.Security.Cryptography;
using System.Text;
using WeChat.Components.Tools;
using Microsoft.Extensions.Caching.Distributed;
using WeChat.API.Models;

namespace WeChat.API.Controllers
{
    public class WeChatController : Controller
    {
        private ILog log = LogManager.GetLogger(Startup.repository.Name, typeof(WeChatController));
        /// <summary>
        /// 获取http请求
        /// </summary>
        private HttpHelper httpHelper = null;
        /// <summary>
        /// redis缓存
        /// </summary>
        private IDistributedCache redisCache;
        //微信账号信息
        private string Appid = "";
        private string Accesstoken = "";
        public WeChatController(IDistributedCache cache)
        {
            httpHelper = new HttpHelper();
            redisCache = cache;
        }
        /// <summary>
        /// 验证微信签名
        /// </summary>
        /// * 将token、timestamp、nonce三个参数进行字典序排序
        /// * 将三个参数字符串拼接成一个字符串进行sha1加密
        /// * 开发者获得加密后的字符串可与signature对比，标识该请求来源于微信。
        /// <returns></returns>
        [HttpGet]
        [Route("api/wechat/wechatcheck")]
        public ActionResult WeChatCheck(string signature, string timestamp, string nonce, string echostr, string token)
        {
            string[] ArrTmp = { "wechat", timestamp, nonce };
            //字典排序
            Array.Sort(ArrTmp);
            string tmpStr = string.Join("", ArrTmp);
            //字符加密
            var sha1 = new EncryptHelper().HmacSha1Sign(tmpStr);
            if (sha1.Equals(signature))
            {
                var accessToken = QueryAccessToken();
                return Content(echostr);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取微信access_token
        /// </summary>
        [HttpPost]
        [Route("api/wechat/getaccesstoken")]
        public string QueryAccessToken()
        {

            HttpItem item = new HttpItem
            {
                URL = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={Appid}&secret={Accesstoken}"
            };
            var accessToken = httpHelper.GetHtml(item);
            var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<WeChatAaccessToken>(accessToken.Html);
            if (!string.IsNullOrEmpty(jObject.access_token))
            {
                redisCache.SetString("AccessToken", jObject.access_token);
            }
            return jObject.access_token;
        }
        public ActionResult GetOpenIdList()
        {
            HttpItem item = new HttpItem
            {
                URL = "https://api.weixin.qq.com/cgi-bin/user/get?" + "access_token=" + Accesstoken + "&next_openid="
            };
            var weChatOpenidlist = httpHelper.GetHtml(item);
            return Content(weChatOpenidlist.Html);
        }
        /// <summary>
        /// 创建菜单
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/wechat/createmenu")]
        public ActionResult CreateMenu([FromBody]WeChatMenu menu)
        {
            HttpItem item = new HttpItem
            {
                URL = $" https://api.weixin.qq.com/cgi-bin/menu/create?access_token={redisCache.GetString("AccessToken")}",
                Postdata = Json(menu).ToString()
            };
            var weChatOpenidlist = httpHelper.GetHtml(item);
            return null;
        }
    }
}