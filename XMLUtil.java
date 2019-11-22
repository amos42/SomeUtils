/*
 * XMLUtil
 *
 * Copyright (c) 2000 - 2013 Samsung Electronics Co., Ltd. All rights reserved.
 *
 * Contact:
 * Gyeongmin Ju <gyeongmin.ju@samsung.com>
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Contributors:
 * - S-Core Co., Ltd
 */
package org.tizen.core.gputil;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.StringWriter;
import java.io.Writer;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import javax.xml.XMLConstants;
import javax.xml.namespace.NamespaceContext;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import javax.xml.transform.OutputKeys;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerConfigurationException;
import javax.xml.transform.TransformerException;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.dom.DOMSource;
import javax.xml.transform.stream.StreamResult;
import javax.xml.xpath.XPath;
import javax.xml.xpath.XPathConstants;
import javax.xml.xpath.XPathExpressionException;
import javax.xml.xpath.XPathFactory;

import org.tizen.core.gputil.Assert;
import org.w3c.dom.Attr;
import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;
import org.w3c.dom.Text;
import org.xml.sax.SAXException;

public class XMLUtil {
    private static final String REGARDLESS_OF_NAMESPACE_XPATH = "/*[local-name()=\'%s\']";

    private static class UniversalNamespaceResolver implements NamespaceContext {
        private Element sourceDocument;
        private String nameSpace;

        public UniversalNamespaceResolver(Element document, String nameSpace) {
            sourceDocument = document;
            this.nameSpace = nameSpace;
        }

        @Override
        public String getNamespaceURI(String prefix) {
            if (prefix == null) {
                throw new IllegalArgumentException("No prefix provided!");
            } else if (prefix.equals(XMLConstants.DEFAULT_NS_PREFIX)) {
                if (nameSpace == null || nameSpace.isEmpty()) {
                    // return sourceDocument.lookupNamespaceURI(null);
                    return sourceDocument.getNamespaceURI();
                } else {
                    return nameSpace;
                }
            } else {
                return sourceDocument.lookupNamespaceURI(prefix);
            }
        }

        @Override
        public String getPrefix(String namespaceURI) {
            return sourceDocument.lookupPrefix(namespaceURI);
        }

        @SuppressWarnings("rawtypes")
        @Override
        public Iterator getPrefixes(String namespaceURI) {
            // TODO Auto-generated method stub
            return null;
        }
    }

    public static boolean existChildElement(Element parentElem) {
        NodeList nodeList = parentElem.getChildNodes();
        for (int i = 0; i < nodeList.getLength(); i++) {
            Node node = nodeList.item(i);
            if (node instanceof Element) {
                return true;
            }
        }
        return false;
    }

    public static boolean isEqualNode(Node a, Node b) {
        String a_name = a.getNodeName();
        String b_name = b.getNodeName();
        if (a_name != null) {
            if (!a_name.equals(b_name)) {
                return false;
            }
        } else if (b_name != null) {
            return false;
        }

        if (a.getAttributes().getLength() != b.getAttributes().getLength()) {
            return false;
        }

        for (int i = 0; i < a.getAttributes().getLength(); i++) {
            Node attrNode = a.getAttributes().item(i);
            if (attrNode instanceof Attr) {
                Node attrNode2 = b.getAttributes().getNamedItem(((Attr) attrNode).getName());
                if (attrNode2 == null) {
                    return false;
                }
                if (!((Attr) attrNode2).getValue().equals(((Attr) attrNode).getValue())) {
                    return false;
                }
            }
        }

        if (a instanceof Element) {
            if (!existChildElement((Element) a)) {
                String a_text = a.getTextContent();
                String b_text = b.getTextContent();
                if (!a_text.equals(b_text)) {
                    return false;
                }
            }
        }

        return true;
    }

    private static void mergeChildNode(Document doc, Element desRoot, Element srcRoot) {
        NodeList nodeList = srcRoot.getChildNodes();
        for (int i = 0; i < nodeList.getLength(); i++) {
            Node node = nodeList.item(i);
            if (node instanceof Element) {
                boolean exist = false;
                Node pos = null;
                NodeList desNodeList = desRoot.getChildNodes();
                for (int j = 0; j < desNodeList.getLength(); j++) {
                    Node desNode = desNodeList.item(j);
                    if (desNode != null && desNode.getNodeName().equals(node.getNodeName())) {
                        if (isEqualNode(node, desNode)) {
                            mergeChildNode(doc, (Element) desNode, (Element) node);
                            exist = true;
                            break;
                        } else {
                            pos = desNode;
                        }
                    }
                }
                if (!exist) {
                    if (pos != null) {
                        desRoot.insertBefore(doc.importNode(node, true), pos.getNextSibling());
                    } else {
                        desRoot.appendChild(doc.importNode(node, true));
                    }
                }
            } else if (node instanceof Text) {
                String text = node.getTextContent();
                if (!text.trim().isEmpty()) {
                    // System.out.println(text);
                }
            }
        }
    }

    private static void removeWhitespace(Node parentNode) {
        NodeList nodeList = parentNode.getChildNodes();
        if (nodeList.getLength() <= 0)
            return;

        for (int i = nodeList.getLength() - 1; i >= 0; i--) {
            Node node = nodeList.item(i);
            if (node instanceof Element) {
                removeWhitespace(node);
            } else if (node instanceof Text) {
                String text = node.getTextContent();
                if (text.trim().isEmpty()) {
                    parentNode.removeChild(node);
                }
            }
        }
    }

    public static Document readXML(InputStream xmlStream, boolean whiteSpace) {
        DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
        factory.setNamespaceAware(true);
        factory.setIgnoringElementContentWhitespace(!whiteSpace);

        DocumentBuilder builder;
        try {
            builder = factory.newDocumentBuilder();
        } catch (ParserConfigurationException e1) {
            // TODO Auto-generated catch block
            e1.printStackTrace();
            return null;
        }

        Document doc = null;
        try {
            doc = builder.parse(xmlStream);
            if (!whiteSpace) {
                removeWhitespace(doc.getDocumentElement());
            }
        } catch (SAXException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }

        return doc;
    }

    public static Document readXML(InputStream xmlStream) {
        return readXML(xmlStream, false);
    }

    public static Document readXML(File xmlFile, boolean whiteSpace) {
        Document doc = null;
        try {
            doc = readXML(new FileInputStream(xmlFile), whiteSpace);
        } catch (FileNotFoundException e) {
            e.printStackTrace();
        }

        return doc;
    }

    public static Document readXML(File xmlFile) {
        return readXML(xmlFile, false);
    }

    public static Document mergeXML(List<File> listOfFiles) {
        DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
        factory.setNamespaceAware(true);
        factory.setIgnoringElementContentWhitespace(true);

        DocumentBuilder builder;
        try {
            builder = factory.newDocumentBuilder();
        } catch (ParserConfigurationException e1) {
            e1.printStackTrace();
            return null;
        }

        Document newDoc = null;

        for (File listOfFile : listOfFiles) {
            Document doc;
            try {
                doc = builder.parse(listOfFile);
            } catch (SAXException e) {
                e.printStackTrace();
                continue;
            } catch (IOException e) {
                e.printStackTrace();
                continue;
            }

            if (doc == null) {
                continue;
            }
            
            removeWhitespace(doc.getDocumentElement());
            
            if (newDoc == null) {
                newDoc = doc;
                continue;
            }

            if (doc != null) {
                mergeChildNode(newDoc, newDoc.getDocumentElement(), doc.getDocumentElement());
            }
        }

        return newDoc;
    }

    public static List<Element> getElementsByTag(Element parent, String tag) {
        Assert.notNull(parent);
        Assert.notNull(tag);

        List<Element> result = new ArrayList<Element>();
        NodeList nodeList = parent.getElementsByTagName(tag);
        for (int i = 0; i < nodeList.getLength(); i++) {
            Node node = nodeList.item(i);
            if (node.getNodeType() == Node.ELEMENT_NODE) {
                result.add((Element) node);
            }
        }
        return result;
    }

    public static Node getDOMNodeNS(Element elem, String xpathStr, String nameSpace) {
        XPathFactory factory = XPathFactory.newInstance();
        XPath xPath = factory.newXPath();
        if (nameSpace != null) {
            xPath.setNamespaceContext(new UniversalNamespaceResolver(elem, nameSpace));
        }

        Object node = null;
        try {
            node = xPath.evaluate(xpathStr, elem, XPathConstants.NODE);
        } catch (XPathExpressionException e) {
            e.printStackTrace();
        }

        if (node instanceof Node) {
            return (Node) node;
        } else {
            return null;
        }
    }

    public static Node getDOMNodeNS(Element elem, String xpathStr) {
        return getDOMNodeNS(elem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
    }

    public static Node getDOMNode(Element elem, String xpathStr) {
        return getDOMNodeNS(elem, xpathStr, null);
    }

    public static NodeList getDOMNodesNS(Element elem, String xpathStr, String nameSpace) {
        XPathFactory factory = XPathFactory.newInstance();
        XPath xPath = factory.newXPath();
        if (nameSpace != null) {
            xPath.setNamespaceContext(new UniversalNamespaceResolver(elem, nameSpace));
        }

        Object node = null;
        try {
            node = xPath.evaluate(xpathStr, elem, XPathConstants.NODESET);
        } catch (XPathExpressionException e) {
            e.printStackTrace();
        }

        if (node instanceof NodeList) {
            return (NodeList) node;
        } else {
            return null;
        }
    }

    public static NodeList getDOMNodesNS(Element elem, String xpathStr) {
        return getDOMNodesNS(elem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
    }

    public static NodeList getDOMNodes(Element elem, String xpathStr) {
        return getDOMNodesNS(elem, xpathStr, null);
    }

    public static String getDOMValueNS(Element elem, String xpathStr, String nameSpace) {
        XPathFactory factory = XPathFactory.newInstance();
        XPath xPath = factory.newXPath();
        if (nameSpace != null) {
            xPath.setNamespaceContext(new UniversalNamespaceResolver(elem, nameSpace));
        }

        Object value;
        try {
            value = xPath.evaluate(xpathStr, elem, XPathConstants.STRING);
        } catch (XPathExpressionException e) {
            e.printStackTrace();
            return null;
        }

        if (value instanceof String) {
            return (String) value;
        } else {
            return null;
        }
    }

    public static String getDOMValueNS(Element elem, String xpathStr) {
        return getDOMValueNS(elem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
    }

    public static String getDOMValue(Element elem, String xpathStr) {
        return getDOMValueNS(elem, xpathStr, null);
    }

    public static void setDOMValueNS(Element elem, String xpathStr, String value, String nameSpace) {
        XPathFactory factory = XPathFactory.newInstance();
        XPath xPath = factory.newXPath();
        if (nameSpace != null) {
            xPath.setNamespaceContext(new UniversalNamespaceResolver(elem, nameSpace));
        }

        Object node = null;
        try {
            node = xPath.evaluate(xpathStr, elem, XPathConstants.NODE);
        } catch (XPathExpressionException e) {
            e.printStackTrace();
        }

        if (node instanceof Node) {
            ((Node) node).setNodeValue(value);
        }
    }

    public static void setDOMValueNS(Element elem, String xpathStr, String value) {
        setDOMValueNS(elem, xpathStr, value, XMLConstants.DEFAULT_NS_PREFIX);
    }

    public static void setDOMValue(Element elem, String xpathStr, String value) {
        setDOMValueNS(elem, xpathStr, value, null);
    }

    public static String getDOMValueNS(Document doc, String xpathStr) {
        return getDOMValueNS(doc.getDocumentElement(), xpathStr);
    }

    public static String getDOMValueNS(Document doc, String xpathStr, String nameSpace) {
        return getDOMValueNS(doc.getDocumentElement(), xpathStr, nameSpace);
    }

    public static Node getDOMNodeNS(Document doc, String xpathStr) {
        return getDOMNodeNS(doc.getDocumentElement(), xpathStr);
    }

    public static Node getDOMNodeNS(Document doc, String xpathStr, String nameSpace) {
        return getDOMNodeNS(doc.getDocumentElement(), xpathStr, nameSpace);
    }

    public static String getDOMValue(Document doc, String xpathStr) {
        return getDOMValue(doc.getDocumentElement(), xpathStr);
    }

    public static Node getDOMNode(Document doc, String xpathStr) {
        return getDOMNode(doc.getDocumentElement(), xpathStr);
    }

    public static NodeList getDOMNodesNS(Document doc, String xpathStr) {
        return getDOMNodesNS(doc.getDocumentElement(), xpathStr);
    }

    public static NodeList getDOMNodes(Document doc, String xpathStr) {
        return getDOMNodes(doc.getDocumentElement(), xpathStr);
    }

    public static void setDOMValue(Document doc, String xpathStr, String value) {
        setDOMValue(doc.getDocumentElement(), xpathStr, value);
    }

    public static void setDOMValueNS(Document doc, String xpathStr, String value) {
        setDOMValueNS(doc.getDocumentElement(), xpathStr, value);
    }

    public static String getDOMValueR(Element elm, String xpathStr) {
        String namespace = elm.getNamespaceURI();
        if (namespace == null) {
            return getDOMValueNS(elm, xpathStr, null);
        } else {
            return getDOMValueNS(elm, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
        }
    }

    public static String getDOMValueT(Document doc, String... tags) {
        Element docElem = doc.getDocumentElement();
        String namespace = docElem.getNamespaceURI();
        if (namespace == null) {
            String xpathStr = convertTags(tags, false);
            return getDOMValueNS(docElem, xpathStr, null);
        } else {
            String xpathStr = convertTags(tags, true);
            return getDOMValueNS(docElem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
        }
    }

    public static Node getDOMNodeT(Document doc, String... tags) {
        Element docElem = doc.getDocumentElement();
        String namespace = docElem.getNamespaceURI();
        if (namespace == null) {
            String xpathStr = convertTags(tags, false);
            return getDOMNodeNS(docElem, xpathStr, null);
        } else {
            String xpathStr = convertTags(tags, true);
            return getDOMNodeNS(docElem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
        }
    }

    public static NodeList getDOMNodesT(Document doc, String... tags) {
        Element docElem = doc.getDocumentElement();
        String namespace = docElem.getNamespaceURI();
        if (namespace == null) {
            String xpathStr = convertTags(tags, false);
            return getDOMNodesNS(docElem, xpathStr, null);
        } else {
            String xpathStr = convertTags(tags, true);
            return getDOMNodesNS(docElem, xpathStr, XMLConstants.DEFAULT_NS_PREFIX);
        }
    }

    private static String convertTags(String[] tags, boolean existNamespace) {
        String ret = "";

        for (int i = 0; i < tags.length; i++) {
            if (tags[i].startsWith("@") || tags[i].split(":").length > 1) {
                // do not change
                ret += String.format("/%s", tags[i]);
            } else {
                // use local-name() to ignore namespace
                ret += String.format(REGARDLESS_OF_NAMESPACE_XPATH, tags[i]);
            }
        }

        return ret;
    }

    public static void writeTo(Document doc, Writer writer) throws IOException, ParserConfigurationException {
        DOMSource source = new DOMSource(doc);
        StreamResult result = new StreamResult(writer);

        TransformerFactory factory = TransformerFactory.newInstance();
        factory.setAttribute("indent-number", new Integer(4));
        Transformer transformer = null;
        try {
            transformer = factory.newTransformer();
            transformer.setOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "no");
            transformer.setOutputProperty(OutputKeys.ENCODING, "UTF-8");
            transformer.setOutputProperty(OutputKeys.METHOD, "xml");
            transformer.setOutputProperty(OutputKeys.INDENT, "yes");
            transformer.setOutputProperty("{http://xml.apache.org/xslt}indent-amount", "4");
        } catch (TransformerConfigurationException e) {
            e.printStackTrace();
            return;
        }

        try {
            transformer.transform(source, result);
        } catch (TransformerException e) {
            e.printStackTrace();
        }
    }

    public static String writeToString(Document doc) {
        StringWriter writer = new StringWriter();
        try {
            writeTo(doc, writer);
        } catch (IOException e) {
            e.printStackTrace();
        } catch (ParserConfigurationException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }

        return writer.getBuffer().toString();
    }

    public static void writeToFile(Document doc, String filePath) {
        try {
            writeTo(doc, new FileWriter(filePath));
        } catch (IOException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        } catch (ParserConfigurationException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
    }
}
