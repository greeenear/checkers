using System;
using option;
using jonson;
using jonson.reflect;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace controller {
    public struct CheckerInfo {
        public Checker checker;
        public int x;
        public int y;

        public static CheckerInfo Mk(Checker checker, int xPos, int yPos) {
            return new CheckerInfo {checker = checker, x = xPos, y = yPos};
        }
    }

    public struct GameState {
        public List<CheckerInfo> checkerInfos;
        public Color whoseMove;

        public static GameState Mk(List<CheckerInfo> info) {
            return new GameState { checkerInfos = info };
        }
    }

    public static class SaveLoad {
        public static JSONType GetJsonType<T>(T type) {
            return Reflect.ToJSON(type, true);
        }

        public static void WriteJson(JSONType jsonType, string path) {
            string output = Jonson.Generate(jsonType);
            try {
                File.WriteAllText(path, output);
            }
            catch (Exception err) {
                Debug.LogError(err.ToString());
            }
        }

        public static T ReadJson<T>(string path, T type) {
            string input = "";
            try {
                input = File.ReadAllText(path);
            }
            catch (Exception err) {
                Debug.LogError(err.ToString());
            }
            var gameStatsRes = Jonson.Parse(input, 1024);
            var loadGameStats =  Reflect.FromJSON(type, gameStatsRes.AsOk());

            return loadGameStats;
        }
    }
}
