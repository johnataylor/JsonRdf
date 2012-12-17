using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using Newtonsoft.Json.Linq;

namespace Proto
{
    public static class RdfJson
    {
        public static JObject Graph2Json(Graph graph)
        {
            JObject json = new JObject();

            foreach (Triple triple in graph.Triples)
            {
                JObject predicate;
                JArray objects;

                JToken p;
                if (json.TryGetValue(triple.Subject.ToString(), out p))
                {
                    predicate = (JObject)p;
                }
                else
                {
                    predicate = new JObject();
                    json.Add(triple.Subject.ToString(), predicate);
                }

                JToken o;
                if (predicate.TryGetValue(triple.Predicate.ToString(), out o))
                {
                    objects = (JArray)o;
                }
                else
                {
                    objects = new JArray();
                    predicate.Add(triple.Predicate.ToString(), objects);
                }

                JObject obj = new JObject();

                if (typeof(IUriNode).IsAssignableFrom(triple.Object.GetType()))
                {
                    obj.Add("type", "uri");
                    obj.Add("value", triple.Object.ToString());
                }
                else if (typeof(ILiteralNode).IsAssignableFrom(triple.Object.GetType()))
                {
                    obj.Add("type", "literal");

                    ILiteralNode literal = (ILiteralNode)triple.Object;

                    if (literal.DataType != null)
                    {
                        obj.Add("datatype", literal.DataType.ToString());
                    }

                    if (literal.Language != null)
                    {
                        obj.Add("lang", literal.Language.ToString());
                    }

                    obj.Add("value", literal.Value);
                }
                else if (typeof(IBlankNode).IsAssignableFrom(triple.Object.GetType()))
                {
                    obj.Add("type", "bnode");
                    obj.Add("value", triple.Object.ToString());
                }

                objects.Add(obj);
            }

            return json;
        }

        public static Graph Json2Graph(JObject json)
        {
            Graph graph = new Graph();

            foreach (JProperty subjectProperty in json.Properties())
            {
                INode subjectNode = graph.CreateUriNode(new Uri(subjectProperty.Name));

                foreach (JProperty predicateProperty in ((JObject)subjectProperty.Value).Properties())
                {
                    INode predicateNode = graph.CreateUriNode(new Uri(predicateProperty.Name));

                    foreach (JObject objectObject in (JArray)predicateProperty.Value)
                    {
                        JToken t;
                        if (!objectObject.TryGetValue("type", out t))
                        {
                            throw new FormatException(string.Format("type is required on {0}, {1}", subjectProperty.Name, predicateProperty.Name));
                        }

                        JToken v;
                        if (!objectObject.TryGetValue("value", out v))
                        {
                            throw new FormatException(string.Format("value is required on {0}, {1}", subjectProperty.Name, predicateProperty.Name));
                        }

                        string type = t.ToString();

                        INode objectNode;

                        switch (type)
                        {
                            case "uri":
                                objectNode = graph.CreateUriNode(new Uri(v.ToString()));
                                break;

                            case "literal":

                                JToken l;
                                if (objectObject.TryGetValue("lang", out l))
                                {
                                    objectNode = graph.CreateLiteralNode(v.ToString(), l.ToString());
                                }
                                else
                                {
                                    JToken d;
                                    if (objectObject.TryGetValue("datatype", out d))
                                    {
                                        objectNode = graph.CreateLiteralNode(v.ToString(), new Uri(d.ToString()));
                                    }
                                    else
                                    {
                                        objectNode = graph.CreateLiteralNode(v.ToString());
                                    }
                                }

                                break;

                            case "bnode":
                                throw new NotImplementedException("blank nodes");

                            default:
                                throw new FormatException(string.Format("type should be uri|literal|bnode on {0}, {1}", subjectProperty.Name, predicateProperty.Name));
                        }

                        graph.Assert(subjectNode, predicateNode, objectNode);
                    }
                }
            }

            return graph;
        }
    }
}
