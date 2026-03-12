using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameViewShotDecoder))]
[CanEditMultipleObjects]
public class GameViewShotDecoder_Editor : Editor
{
    private GameViewShotDecoder SSDecoder;

    SerializedProperty FastModeProp;
    SerializedProperty AsyncModeProp;

    SerializedProperty ReceivedTexture2DProp;

    SerializedProperty DecodedFilterModeProp;
    SerializedProperty DecodedWrapModeProp;

    SerializedProperty OnReceivedTexture2DProp;

    SerializedProperty labelProp;

    void OnEnable()
    {

        FastModeProp = serializedObject.FindProperty("FastMode");
        AsyncModeProp = serializedObject.FindProperty("AsyncMode");

        ReceivedTexture2DProp = serializedObject.FindProperty("ReceivedTexture2D");

        DecodedFilterModeProp = serializedObject.FindProperty("DecodedFilterMode");
        DecodedWrapModeProp = serializedObject.FindProperty("DecodedWrapMode");

        OnReceivedTexture2DProp = serializedObject.FindProperty("OnReceivedTexture2D");

        labelProp = serializedObject.FindProperty("label");
    }

    // Update is called once per frame
    public override void OnInspectorGUI()
    {
        if(SSDecoder==null) SSDecoder= (GameViewShotDecoder)target;

        serializedObject.Update();

        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            {
                //Header
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 15;

                Texture2D backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0.09019608f, 0.09019608f, 0.2745098f));
                backgroundTexture.Apply();
                style.normal.background = backgroundTexture;

                GUILayout.BeginHorizontal();
                GUILayout.Label("(( FMETP STREAM CORE V2 ))", style);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("- Settings");

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(FastModeProp, new GUIContent("Fast Decode Mode"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.yellow;
                GUILayout.Label(" Experiment for Mac, Windows, Android (Forced Enabled on iOS)", style);
                GUILayout.EndHorizontal();

                if (SSDecoder.FastMode)
                {
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(AsyncModeProp, new GUIContent("Async (multi-threading)"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("- Decoded");

            if (SSDecoder.ReceivedTexture2D != null)
            {
                GUILayout.Label("Preview " + " ( " + SSDecoder.ReceivedTexture2D.width + " x " + SSDecoder.ReceivedTexture2D.height + " ) ");
            }
            else
            {
                GUILayout.Label("Preview (Empty)");
            }


            GUILayout.BeginVertical("box");
            {
                const float maxLogoWidth = 430.0f;
                EditorGUILayout.Separator();
                float w = EditorGUIUtility.currentViewWidth;
                Rect r = new Rect();
                r.width = Math.Min(w - 40.0f, maxLogoWidth);
                r.height = r.width / 4.886f;
                Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
                r.x = r2.x;
                r.y = r2.y;
                if (SSDecoder.ReceivedTexture2D != null)
                {
                    GUI.DrawTexture(r, SSDecoder.ReceivedTexture2D, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.DrawTexture(r, new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false), ScaleMode.ScaleToFit);
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(DecodedFilterModeProp, new GUIContent("Filter Mode"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(DecodedWrapModeProp, new GUIContent("Wrap Mode"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(OnReceivedTexture2DProp, new GUIContent("OnReceivedTexture2D"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();


        GUILayout.Space(10);
        GUILayout.BeginVertical("box");
        {
            GUILayout.Label("- Pair Encoder & Decoder ");
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(labelProp, new GUIContent("label"));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
