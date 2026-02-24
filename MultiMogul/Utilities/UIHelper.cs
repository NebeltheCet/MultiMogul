using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MultiMogul.Utilities;

public class UIHelper {
	public static TMP_InputField CreateInputFieldFromInstance(TMP_InputField instance, string name, UnityEngine.Vector2 pos, string text, string hint, bool numbersOnly) {
		TMP_InputField newInput = UnityEngine.Object.Instantiate(instance, instance.transform.parent);
		newInput.name = name;
		newInput.text = text;

		RectTransform rectTransform = newInput.GetComponent<RectTransform>();
		if(rectTransform != null)
			rectTransform.anchoredPosition = pos;
		
		TMP_Text placeholder = newInput.placeholder as TMP_Text;
		if(placeholder != null) 
			placeholder.text = hint;
		
		if (numbersOnly)
			newInput.contentType = TMP_InputField.ContentType.IntegerNumber;

		return newInput;
	}
}
