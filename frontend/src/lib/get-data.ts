export const getDataFromServer = <T>(dataAttributeName: string): T | null => {
  const element = document.querySelector(`[${dataAttributeName}]`);

  if (!element) return null;

  const attribute = element.attributes.getNamedItem(dataAttributeName);

  if (!attribute) return null;

  const parsedJson = JSON.parse(attribute.nodeValue || '') as T;

  // remove temporary element from DOM
  element.remove();

  if (!parsedJson) return null;

  return parsedJson;
};
