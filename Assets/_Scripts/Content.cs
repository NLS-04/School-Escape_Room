using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

#nullable enable

public class Content
{
    string jsonPath;
    public readonly string relativPath = "/_Scripts/Text_Content.json";

    Dictionary<uint, TextSegment> textDict = new Dictionary<uint, TextSegment>();

    public Content(bool doDebuging=false){
        jsonPath = Application.dataPath + relativPath;
        
        readParseJson(jsonPath, ref textDict);

        if(doDebuging) {
            TextSegment[] TS = new TextSegment[3] {
                new TextSegment(1, "Content_1", "textAnswer_1", new Answer( false, sA:"singleAnswer")),
                new TextSegment(2, "Content_2", "textAnswer_2", new Answer( true, mCs:new Choice[2] { new Choice("ChoiceSentence_1", true), new Choice("ChoiceSentence_2", false) } )),
                new TextSegment(3, "Content_3", "textAnswer_3", new Answer( true, mCs:new Choice[2] { new Choice("ChoiceSentence_1", false), new Choice("ChoiceSentence_2", true) } ))
            };

            TextCollector TC = new TextCollector( TS );
            foreach(var seg in TS) Debug.Log(seg);
            writeToJson(jsonPath, TC);
        }
    }

    // read the Text_Content.json and parses it into the textDict
    public void readParseJson(string path, ref Dictionary<uint, TextSegment> temp) {
        using (StreamReader sr = new StreamReader(path)) {
            string json = sr.ReadToEnd();

            TextCollector collector = JsonUtility.FromJson<TextCollector>(json);

            if (collector.segments is null) return;
            
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
        if ( !textDict.ContainsKey(segmentID) ) return null;
        return textDict[segmentID];
    }

    [Serializable]
    class TextCollector {
        public TextSegment[] segments;

        public TextCollector(TextSegment[] s) { segments = s; }
    }
}

[Serializable]
public class TextSegment {
    public uint id;
    public string content;
    public string textAnswer;
    public Answer answer;
    
    public TextSegment(uint xid, string c, string tA, Answer a) {
        id=xid; content=c; textAnswer=tA; answer=a;
    }
    public override string ToString() {
        return $"id:{id}, content:{content}, textAnswer:{textAnswer}, expectedAnswer:{{\n{answer}\n}}";
    }
}

[Serializable]
public class Answer {
    public bool isMultipleChoice;
    public string?   singleAnswer;
    public Choice?[] multipleChoices;

    public Answer(bool iMC, string? sA=null, Choice?[] mCs=null) {
        isMultipleChoice = iMC;
        singleAnswer = sA;
        multipleChoices = mCs;
    }

    public override string ToString() {
        if(!isMultipleChoice) return $"\tsingleAnswer:{singleAnswer}";
        else {
            string output = "\tChoice:[\n\t\t";
            foreach(Choice choi in multipleChoices) {
                output += choi.ToString() + ";\n\t\t";
            }
            return output.Remove(output.Length-4, 4) + "\n\t]";
        }
    }
}

[Serializable]
public class Choice {
    public string sentence;
    public bool isCorrect;

    public Choice(string s, bool i){
        sentence=s;
        isCorrect=i;
    }

    public override string ToString() {
        return $"isCorrect:{isCorrect}, sentence:{sentence}";
    }
}