using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class Content
{
    string jsonPath;
    public readonly string relativPath = "/_Scripts/Text_Content.json";

    Dictionary<uint, TextSegment> textDict = new Dictionary<uint, TextSegment>();

    [Serializable]
    class TextCollector {
        public TextSegment[] segments;

        public TextCollector(TextSegment[] s) { segments = s; }
    }

    public Content(){
        jsonPath = Application.dataPath + relativPath;
        
        readParseJson(jsonPath, ref textDict);
    }

    // read the Text_Content.json and parses it into the textDict
    public void readParseJson(string path, ref Dictionary<uint, TextSegment> temp) {
        using (StreamReader sr = new StreamReader(path)) {
            string json = sr.ReadToEnd();

            TextCollector collector = JsonUtility.FromJson<TextCollector>(json);
        
            foreach(TextSegment segment in collector.segments) {
                textDict.Add(segment.id, segment);
            }
        }
    }

    // write to Text_Content.json a TextCollector
    private void writeToJson(string path, TextCollector content) {
        string json = JsonUtility.ToJson(content, true);
        File.WriteAllText(path, json);
    }

    // returns a Textsegment or null for a given id
    public TextSegment? GetTextSegment(uint segmentID) {
        if ( !textDict.ContainsKey(segmentID) )
            return null;
        return textDict[segmentID];
    }
}

[Serializable]
public class TextSegment {
    public uint id;
    public string content;
    public string textAnswer;
    public string expectedAnswer;
    
    public TextSegment(uint xid, string c, string tA, string eA) {
        id=xid; content=c; textAnswer=tA; expectedAnswer=eA;
    }
    public override string ToString() {
        return $"id:{id}, content:{content}, textAnswer:{textAnswer}, expectedAnswer:{expectedAnswer}";
    }
}