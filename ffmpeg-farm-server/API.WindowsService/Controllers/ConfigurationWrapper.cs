﻿using System.Configuration;
using API.WindowsService.Filters;

namespace API.WindowsService.Controllers
{
    /// <summary>
    /// configuration wrapper
    /// </summary>
    [ApiAuthorization]
    public static class ConfigurationWrapper
    {
        public static string FFmpeg32 { get { return ConfigurationManager.AppSettings["FFmpeg_3_2"]; } }
        public static string FFmpeg341 { get { return ConfigurationManager.AppSettings["FFmpeg_3_4_1"]; } }
    }
}