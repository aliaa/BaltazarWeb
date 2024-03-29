﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb
{
    public static class Consts
    {
        public const int INITIAL_COIN = 10;
        public const int QUESTION_COIN_COST = 2;
        public const int ANSWER_DEFAULT_PRIZE = 1;
        public const int INVITE_PRIZE = 5;
        public const int INVITED_PRIZE = 3;
        public const int PROFILE_COMPLETE_PRIZE = 5;
        public const string UPLOAD_IMAGE_DIR = "Uploads\\Image";
        public const string UPLOAD_VOICE_DIR = "Uploads\\Voice";
        public const string UPLOAD_VIDEO_DIR = "Uploads\\Video";
        public const string UPLOAD_FILE_DIR = "Uploads\\File";
        public static readonly string[] SEASON_NAMES = new string[] { "بهار", "تابستان", "پاییز", "زمستان" };
    }
}
