using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static TMPro.TMP_InputField;

namespace MultiMogul.Utilities;

public class UIHelper {
	public static TMP_InputField CreateInputFieldFromInstance(TMP_InputField instance, string name, UnityEngine.Vector2 pos, string text, string hint, ContentType contentType = ContentType.Standard) {
		TMP_InputField newInput = UnityEngine.Object.Instantiate(instance, instance.transform.parent);
		newInput.name = name;
		newInput.text = text;

		RectTransform rectTransform = newInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = pos;
		}
		
		TMP_Text placeholder = newInput.placeholder as TMP_Text;
		if (placeholder != null) {
			placeholder.text = hint;
		}

		if (contentType != ContentType.Standard) {
			newInput.contentType = contentType;
		}

		return newInput;
	}
}
