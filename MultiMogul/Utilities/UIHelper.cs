using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using static TMPro.TMP_InputField;

namespace MultiMogul.Utilities;

public class UIHelper {
	public static TMP_InputField CreateInputFieldFromInstance(TMP_InputField instance, string name, string text, string hint, ContentType contentType = ContentType.Standard) {
		TMP_InputField newInput = UnityEngine.Object.Instantiate(instance);
		newInput.name = name;
		newInput.text = text;

		TMP_Text placeholder = newInput.placeholder as TMP_Text;
		if (placeholder != null) {
			placeholder.text = hint;
		}

		if (contentType != ContentType.Standard) {
			newInput.contentType = contentType;
		}

		LocalizeStringEvent localizeEvent = placeholder.GetComponent<LocalizeStringEvent>();
		if (localizeEvent != null) {
			localizeEvent.enabled = false;
		}

		return newInput;
	}
}
