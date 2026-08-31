import type {
  PlayerNetworkEdge,
  PlayerNetworkNode,
  PlayerNetworkResponse,
  RelationshipConfidence,
} from "./api.ts";

const confidenceRank: Record<RelationshipConfidence, number> = {
  LOW: 0,
  MEDIUM: 1,
  HIGH: 2,
  VERY_HIGH: 3,
};

export interface PositionedNetworkNode extends PlayerNetworkNode {
  x: number;
  y: number;
  score: number;
}

export function filterNetwork(
  network: PlayerNetworkResponse,
  minimumScore: number,
  minimumConfidence: RelationshipConfidence,
): { nodes: PlayerNetworkNode[]; edges: PlayerNetworkEdge[] } {
  const edges = network.edges.filter(
    (edge) =>
      edge.relationshipScore >= minimumScore &&
      confidenceRank[edge.relationshipConfidence] >=
        confidenceRank[minimumConfidence],
  );
  const included = new Set([
    network.center.puuid,
    ...edges.flatMap((edge) => [edge.sourcePuuid, edge.targetPuuid]),
  ]);
  return {
    nodes: network.nodes.filter((node) => included.has(node.puuid)),
    edges,
  };
}

export function positionNetworkNodes(
  nodes: PlayerNetworkNode[],
  edges: PlayerNetworkEdge[],
): PositionedNetworkNode[] {
  const center = nodes.find((node) => node.isCenter);
  if (!center) return [];
  const others = nodes.filter((node) => !node.isCenter);
  const scores = new Map(
    edges.map((edge) => [
      edge.sourcePuuid === center.puuid ? edge.targetPuuid : edge.sourcePuuid,
      edge.relationshipScore,
    ]),
  );

  return [
    { ...center, x: 400, y: 250, score: 100 },
    ...others.map((node, index) => {
      const angle =
        (2 * Math.PI * index) / Math.max(1, others.length) - Math.PI / 2;
      const radius = others.length <= 8 ? 175 : 205;
      return {
        ...node,
        x: 400 + Math.cos(angle) * radius,
        y: 250 + Math.sin(angle) * radius,
        score: scores.get(node.puuid) ?? 0,
      };
    }),
  ];
}

export function networkNodeRadius(score: number, isCenter: boolean): number {
  return isCenter
    ? 30
    : 16 + Math.round(Math.max(0, Math.min(100, score)) / 10);
}

export function networkEdgeWidth(score: number): number {
  return 1 + Math.max(0, Math.min(100, score)) / 25;
}
