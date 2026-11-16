"""The heads of the reader on a frozen encoder (docs/research/core-logic/specs/reader.md, rung 4, "One pass, several heads").

X is the stitched sequence (T pieces x 768). Shared: Z = dropout(GELU(Linear 768->256 (X + field embedding))) and
c = GELU(Linear 768->256 (note vector)). For a query q: u = Z (W q) / 16 with one shared 256 x 256 matrix W; the evidence
scores are sigmoid(u); the pooled vector is v = softmax(u) Z.

The claim type and the requests are scored by each class's own weights over [v; c] (16 November 2026). The spec's one
shared Linear on [v; q; c] could not fit even the training notes: c is the same for every class and q adds only a bias,
so whether a class is present had to come through one shared direction of the pooled vector (on the 493 pilot notes it
stayed at 12 to 20 % top-1 on its own training notes, where per-class weights reached 88 %).
"""
from __future__ import annotations

import torch
from torch import nn

from reader_data import ACTORS, DISPUTERS, POLARITIES, Space

H = 768
D = 256


class Heads(nn.Module):
    def __init__(self, space: Space, dropout: float = 0.1):
        super().__init__()
        self.space = space
        self.field = nn.Embedding(4, H)
        self.proj = nn.Linear(H, D)
        self.note = nn.Linear(H, D)
        self.drop = nn.Dropout(dropout)
        self.w = nn.Linear(D, D, bias=False)
        self.claim_q = nn.Parameter(torch.randn(len(space.claims), D) * 0.02)
        self.claim_w = nn.Parameter(torch.zeros(len(space.claims), 2 * D))
        self.claim_b = nn.Parameter(torch.zeros(len(space.claims)))
        self.element_q = nn.Parameter(torch.randn(len(space.elements), D) * 0.02)
        self.polarity = nn.Linear(2 * D, len(POLARITIES))
        self.actor = nn.Linear(2 * D, len(ACTORS))
        self.disputer = nn.Linear(2 * D, len(DISPUTERS))
        self.request_q = nn.Parameter(torch.randn(len(space.requests), D) * 0.02)
        self.request_w = nn.Parameter(torch.zeros(len(space.requests), 2 * D))
        self.request_b = nn.Parameter(torch.zeros(len(space.requests)))
        self.slot_q = nn.Parameter(torch.randn(len(space.class_slots), D) * 0.02)
        self.slot_out = nn.ModuleList([nn.Linear(2 * D, len(space.slot_classes(s))) for s in space.class_slots])
        self.party_q = nn.Parameter(torch.randn(len(space.party_claims), D) * 0.02)
        self.party_out = nn.ModuleList([nn.Linear(2 * D, len(space.roles_of[c])) for c in space.party_claims])
        self.role_vec = nn.Embedding(len(space.value_roles), D)
        self.role_mlp = nn.Sequential(nn.Linear(3 * D, D), nn.GELU(), nn.Linear(D, 1))

    def encode(self, tokens: torch.Tensor, fields: torch.Tensor, vector: torch.Tensor):
        z = self.drop(nn.functional.gelu(self.proj(tokens + self.field(fields))))
        c = nn.functional.gelu(self.note(vector))
        return z, c

    def attend(self, z: torch.Tensor, queries: torch.Tensor):
        """Token logits u (Q x T) and pooled vectors v (Q x D) for a set of queries."""
        u = (z @ self.w(queries).T).T / 16.0
        v = torch.softmax(u, dim=-1) @ z
        return u, v

    def forward(self, tokens, fields, vector, values=None):
        """One note. `values`: list of index arrays (the pieces of each parser value). Returns every head's logits."""
        z, c = self.encode(tokens, fields, vector)
        out = {}
        u, v = self.attend(z, self.claim_q)
        out["claim_u"] = u
        out["claim"] = (torch.cat([v, c.expand(len(v), -1)], dim=-1) * self.claim_w).sum(-1) + self.claim_b
        u, v = self.attend(z, self.element_q)
        x = torch.cat([v, self.element_q], dim=-1)
        out["element_u"], out["polarity"], out["actor"], out["disputer"] = u, self.polarity(x), self.actor(x), self.disputer(x)
        u, v = self.attend(z, self.request_q)
        out["request_u"], out["request"] = u, (torch.cat([v, c.expand(len(v), -1)], dim=-1) * self.request_w).sum(-1) + self.request_b
        u, v = self.attend(z, self.slot_q)
        x = torch.cat([v, self.slot_q], dim=-1)
        out["slot_u"], out["slot"] = u, [head(x[i]) for i, head in enumerate(self.slot_out)]
        u, v = self.attend(z, self.party_q)
        x = torch.cat([v, self.party_q], dim=-1)
        out["party_u"], out["party"] = u, [head(x[i]) for i, head in enumerate(self.party_out)]
        # Value roles: z of the value's pieces, the mean of 16 pieces each side, and the role vector.
        out["value_ctx"] = []
        t = z.shape[0]
        for idx in values or []:
            if len(idx) == 0:
                out["value_ctx"].append(None)
                continue
            a, b = int(idx.min()), int(idx.max()) + 1
            zv = z[a:b].mean(0)
            around = torch.cat([z[max(0, a - 16):a], z[b:min(t, b + 16)]], dim=0)
            ctx = around.mean(0) if len(around) else torch.zeros_like(zv)
            out["value_ctx"].append(torch.cat([zv, ctx]))
        return out

    def role_scores(self, ctx: torch.Tensor, roles: torch.Tensor) -> torch.Tensor:
        r = self.role_vec(roles)
        return self.role_mlp(torch.cat([ctx.expand(len(roles), -1), r], dim=-1)).squeeze(-1)
