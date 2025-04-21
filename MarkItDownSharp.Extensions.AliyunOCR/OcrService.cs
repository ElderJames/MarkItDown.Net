using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aliyun.Acs.Core.Http;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MarkItDownSharp.Services;

namespace MarkItDownSharp.Extensions.AliyunOCR
{
    public class OcrService : IOcrService
    {
        private readonly IClientProfile _profile;

        public OcrService(IOptions<AliyunOptions> options)
        {
            _profile = DefaultProfile.GetProfile(
                "cn-hangzhou",  //地域ID
                options.Value.AccessKeyId,  //AccessKey ID
                options.Value.AccessKeySecret);//AccessKey Secret    
        }

        public Task<string> ExtractTextAsync(byte[] imageData)
        {
            DefaultAcsClient client = new DefaultAcsClient(_profile);
            CommonRequest request = new CommonRequest();
            request.Method = MethodType.POST;
            request.Domain = "ocr-api.cn-hangzhou.aliyuncs.com";
            request.Version = "2021-07-07";
            request.Action = "RecognizeAllText";
            request.AddQueryParameters("Type", "Advanced");
            request.SetContent(imageData, "utf-8", FormatType.RAW);

            //发送短信
            CommonResponse response = client.GetCommonResponse(request);
            // var result = JsonSerializer.Deserialize<Rootobject>(response.Data);

            // var data = result?.Data.SubImages[0].KvInfo.Data;

            return Task.FromResult(response.Data);
        }
    }


    public class Rootobject
    {
        public string RequestId { get; set; }
        public Data Data { get; set; }
    }

    public class Data
    {
        public Subimage[] SubImages { get; set; }
        public int Height { get; set; }
        public int SubImageCount { get; set; }
        public int Width { get; set; }
    }

    public class Subimage
    {
        public int SubImageId { get; set; }
        public string Type { get; set; }
        public int Angle { get; set; }
        public Kvinfo KvInfo { get; set; }
        public Qualityinfo QualityInfo { get; set; }
    }

    public class Kvinfo
    {
        public int KvCount { get; set; }
        public Data1 Data { get; set; }
        public Kvdetails KvDetails { get; set; }
    }

    public class Data1
    {
        public string address { get; set; }
        public string ethnicity { get; set; }
        public string sex { get; set; }
        public string name { get; set; }
        public string idNumber { get; set; }
        public string birthDate { get; set; }
        public string issueAuthority { get; set; }
        public string validPeriod { get; set; }
    }

    public class Kvdetails
    {
        public Address address { get; set; }
        public Ethnicity ethnicity { get; set; }
        public Sex sex { get; set; }
        public Name name { get; set; }
        public Idnumber idNumber { get; set; }
        public Birthdate birthDate { get; set; }
    }

    public class Address
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Ethnicity
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Sex
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Name
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Idnumber
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Birthdate
    {
        public string KeyName { get; set; }
        public int ValueAngle { get; set; }
        public string Value { get; set; }
        public int KeyConfidence { get; set; }
        public int ValueConfidence { get; set; }
    }

    public class Qualityinfo
    {
        public bool IsCopy { get; set; }
    }

}
