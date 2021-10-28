using System.Collections.ObjectModel;
using option;
using jonson;
using jonson.reflect;
using System.IO;
using System.Collections.Generic;

namespace controller {
    public struct CheckerInfo {
        public Checker checker;
        public int x;
        public int y;

        public static CheckerInfo Mk(Checker checker, int xPos, int yPos) {
            return new CheckerInfo {checker = checker, x = xPos, y = yPos};
        }
    }
    public struct JsonObject {
        public List<CheckerInfo> checkerInfos;
        public Color whoseMove;

        public static JsonObject Mk(List<CheckerInfo> info) {
            return new JsonObject { checkerInfos = info };
        }
    }

    public static class SaveLoad {
        public static JSONType GetJsonType<T>(T type) {
            return Reflect.ToJSON(type, true);
        }

        public static void WriteJson(JSONType jsonType, string path) {
            string output = Jonson.Generate(jsonType);
            File.WriteAllText(path, output);
        }

        public static T ReadJson<T>(string path, T type) {
            string input = File.ReadAllText(path);
            Result<JSONType, JSONError> gameStatsRes = Jonson.Parse(input, 1024);
            var loadGameStats =  Reflect.FromJSON(type, gameStatsRes.AsOk());

            return loadGameStats;
        }
    }
}
