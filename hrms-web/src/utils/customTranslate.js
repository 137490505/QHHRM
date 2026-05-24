import zhTranslations from 'bpmn-js-i18n-zh/lib/bpmn-js/index.js';

export function customTranslate(template, replacements) {
  replacements = replacements || {};

  // Translate
  template = zhTranslations[template] || template;

  // Replace
  return template.replace(/{([^}]+)}/g, function(_, key) {
    return replacements[key] || '{' + key + '}';
  });
}

export default {
  __init__: ['translate'],
  translate: ['value', customTranslate]
};
