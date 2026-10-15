using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using static TMPro.TMP_InputField;

namespace MultiMogul.Utilities;

// NOTE: MineMogul developers hinted at a new UI System soon, we may have to abstract this a bit to support future updates.
public class UIHelper {
	public static TMP_InputField CreateInputFieldFromInstance(TMP_InputField instance, string name, string text, string hint, ContentType contentType = ContentType.Standard) {
		TMP_InputField newInput = Object.Instantiate(instance);
		Object.DontDestroyOnLoad(newInput);

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
