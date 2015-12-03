﻿using UnityEngine;
using LitJson;
using System;
using System.Collections.Generic;
using System.Text;

[ExecuteInEditMode]
public class Headmesh : MonoBehaviour {

    public Morph[] Morphs
    {
        get { return _Morphs; }
    }
    [SerializeField]
    private Morph[] _Morphs;

    public string Name { get; set; }

    public string DatafilePath
    {
        get
        {
            return _DatafilePath;
        }
    }
    [SerializeField]
    private string _DatafilePath;

    public string PrototypePath
    {
        get
        {
            return _PrototypePath;
        }
    }
    private string _PrototypePath;

    public bool Modified
    {
        get
        {
            return _Modified;
        }
    }
    [SerializeField]
    private bool _Modified = false;

    public SkinnedMeshRenderer SkinnedRenderer;

    public void LoadFile(string path)
    {
        if (path == null || path.Trim().Equals(""))
        {
            Debug.Log("Loaded Invalid Path");
            return;
        }
            
        Debug.Log("Loading Datafile: " + path);
        MorphJsonType type = ReadMorphFile(path);
        _DatafilePath = path;
        _PrototypePath = type.Prototype;
        _Morphs = type.Morphs;
        for (int x = 0; x < _Morphs.Length; x++)
            SetMorphValue(x, (float)Morphs[x].Value); // Sets the value on the skinned mesh
        Name = type.Name;

        _Modified = false;
    }

    public string WriteJson(MorphSaveType prototype)
    {
        MorphJsonType type = new MorphJsonType();
        type.Morphs = Morphs;
        type.Name = Name;
        if (prototype == MorphSaveType.Derivative)
            type.Prototype = DatafilePath;
        else if (prototype == MorphSaveType.Sibling)
            type.Prototype = PrototypePath;
        else if (prototype == MorphSaveType.Absolute)
            type.Prototype = "";

        StringBuilder sb = new StringBuilder();
        JsonWriter wr = new JsonWriter(sb);
        wr.PrettyPrint = true;
        JsonMapper.ToJson(type, wr);
        return sb.ToString();
    }

    public enum MorphSaveType
    {
        Absolute,   // Saves file absolutely, containing all data necessary to contruct headmesh
        Derivative, // Saves file with prototype set to the headmesh at DatafilePath.  Only changed values are saved.
        Sibling     // Saves file with prototype set to the prototype of the headmesh at DatafilePath.  Only changed values are saved.
    }

    private static MorphJsonType ReadMorphFile(string DataPath, bool add_elements = false)
    {
        TextAsset Data = Resources.Load<TextAsset>(DataPath);
        if (Data == null) // Can't find raw
        {
            MorphJsonType morph = new MorphJsonType();
            morph.Morphs = new Morph[0];
            return morph;
        }

        string json = Data.text;
        MorphJsonType raw = JsonMapper.ToObject<MorphJsonType>(json);

        bool has_prototype = !raw.Prototype.Equals("");

        if (has_prototype)
        {
            string[] split = DataPath.Split('/');
            Array.Resize<string>(ref split, split.Length - 1);

            TextAsset prototype = Resources.Load<TextAsset>(raw.Prototype);
            if (prototype == null) // Can't find prototype
            { 
                MorphJsonType morph = new MorphJsonType();
                morph.Morphs = new Morph[0];
                return morph;
            }

            MorphJsonType ptype = ReadMorphFile(raw.Prototype, add_elements);
            if (raw.Name == null)
                raw.Name = ptype.Name;
            Resources.UnloadAsset(prototype);prototype = null;

            List<Morph> temp = new List<Morph>(ptype.Morphs.Length);
            for (int x = 0; x < ptype.Morphs.Length; x++)
            {
                Morph m = ptype.Morphs[x];
                bool found = false;
                foreach (Morph q in raw.Morphs)
                {
                    if (q.NameInternal.Equals(m.NameInternal))
                    {
                        m.Value = q.Value;
                        m.Name = q.Name;
                        m.Category = q.Category;
                        m.HasNegativeValues = q.HasNegativeValues;
                        found = true;

                        temp.Add(m);
                        break;
                    }
                }
                if(!found && add_elements)
                    temp.Add(m);
            }
            ptype.Morphs = temp.ToArray();
            return ptype;
        }
        else
            return raw;
    }
    
    public void SetMorphValue(int morph_index, float value)
    {
        Morph morph = Morphs[morph_index];
        if (!morph.HasNegativeValues && value < 0)
            return;
        value = Mathf.Clamp(value,-1,1);

        string shapename = morph.NameInternal;
        if(morph.HasNegativeValues)
        {
            if (value > 0)
                shapename += "_pos";
            else
                shapename += "_neg";
        }

        Mesh m = SkinnedRenderer.sharedMesh;
        for(int x=0;x<m.blendShapeCount;x++)
        {
            if (m.GetBlendShapeName(x).Equals(shapename))
            {
                SkinnedRenderer.SetBlendShapeWeight(x, Mathf.Abs(value)*100);
                break;
            }
        }
        
        Morphs[morph_index].Value = value;
        _Modified = true;
    }

    public void Randomize()
    {
        for (int x = 0; x < Morphs.Length; x++)
        {
            float min = Morphs[x].HasNegativeValues ? -1 : 0;
            if(!Morphs[x].NameInternal.StartsWith("hairline"))
                SetMorphValue(x, UnityEngine.Random.Range(min, 1));
        }
    }

    public double GetMorphValue(int morph_index)
    {
        return Morphs[morph_index].Value;
    }

    [System.Serializable]
    public struct Morph
    {
        public string Name;
        public string NameInternal;
        public string Category;
        public bool HasNegativeValues;
        public double Value;
    }

    private struct MorphJsonType
    {
        public string Name;
        public string Prototype;
        public Morph[] Morphs;
    }
}
