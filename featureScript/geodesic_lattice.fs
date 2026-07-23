FeatureScript 2985;
import(path : "onshape/std/common.fs", version : "2985.0");

annotation { "Feature Type Name" : "Geodesic lattice" }
export const geodesicLattice = defineFeature(function(context is Context, id is Id, definition is map)
precondition // input parameters
{
    annotation { "Name" : "Wing solid", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
    definition.wing is Query;

    annotation { "Name" : "Chord plane", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
    definition.plane is Query;

    annotation { "Name" : "Cell size" }
    isLength(definition.cellSize, { (millimeter) : [2, 18, 300] } as LengthBoundSpec);

    annotation { "Name" : "Web thickness" }
    isLength(definition.webThickness, { (millimeter) : [0.2, 0.6, 5] } as LengthBoundSpec);

    annotation { "Name" : "Skin thickness" }
    isLength(definition.skinThickness, { (millimeter) : [0.4, 1.0, 8] } as LengthBoundSpec);    

    annotation { "Name" : "Lightening holes", "Default" : true }
    definition.lightenWebs is boolean;

    annotation { "Name" : "Hole border" }
    isLength(definition.holeBorder, { (millimeter) : [0.3, 1.5, 10] } as LengthBoundSpec);

    annotation { "Name" : "Diagonal half-angle" }
    isAngle(definition.angle, { (degree) : [5, 45, 85] } as AngleBoundSpec);

    annotation { "Name" : "Keep input wing", "Default" : false }
    definition.keepWing is boolean;

    annotation { "Name" : "Close exterior (tip) end", "Default" : true }
    definition.capTip is boolean;

    annotation { "Name" : "Close interior (root) end", "Default" : false }
    definition.capRoot is boolean;
}
{
    // get chord plane and its axes
    const pl    = evPlane(context, { "face" : definition.plane });
    const nDir  = pl.normal;
    const xDir  = pl.x;
    const yDir  = cross(nDir, xDir);

    // find wing center, which will be the base of the temp coord system
    const wingBox    = evBox3d(context, { "topology" : definition.wing, "tight" : false });
    const center0 = (wingBox.minCorner + wingBox.maxCorner) / 2;

    const wbox   = evBox3d(context, { "topology" : definition.wing, "cSys" : coordSystem(center0, xDir, nDir), "tight" : false }); // remake box in temp coord system
    const extX   = wbox.maxCorner[0] - wbox.minCorner[0];   
    const extY   = wbox.maxCorner[1] - wbox.minCorner[1];   
    const extZ   = wbox.maxCorner[2] - wbox.minCorner[2];  //height

    const cs     = definition.cellSize;
    const tw     = definition.webThickness;

    const center = center0 // recalculating center in temp coord system in case the wing is rotated
        + xDir * ((wbox.minCorner[0] + wbox.maxCorner[0]) / 2)
        + yDir * ((wbox.minCorner[1] + wbox.maxCorner[1]) / 2)
        + nDir * ((wbox.minCorner[2] + wbox.maxCorner[2]) / 2);
    const reach  = sqrt(extX ^ 2 + extY ^ 2) / 2 + cs;   // half the in-plane diagonal (+margin)
    const height = extZ + 2 * cs;  

    var chordDir = xDir;                                   // chord = the shorter in-plane axis
    var spanDir  = yDir;                                   // span  = the longer  in-plane axis
    var spanIdx  = 1;                                      // spanDir's index in the (xDir,yDir,nDir) box frame
    if (extY < extX) { chordDir = yDir; spanDir = xDir; spanIdx = 0; } // change incase the wing is wider than it is long

    // base for lattice
    const familyA = makeFamily(context, id, "A",  definition.angle, center, xDir, nDir, reach, height, cs, tw);
    const familyB = makeFamily(context, id, "B", -definition.angle, center, xDir, nDir, reach, height, cs, tw);

    if (definition.lightenWebs)
    {
        const hb = definition.holeBorder;

        // Cutters
        const cutT = tw + 0.2 * millimeter;
        const cutA = makeFamily(context, id, "CA",  definition.angle, center, xDir, nDir, reach, height, cs, cutT);
        const cutB = makeFamily(context, id, "CB", -definition.angle, center, xDir, nDir, reach, height, cs, cutT);

        const rotA = rotationAround(line(vector(0, 0, 0) * meter, nDir), definition.angle);
        const uA   = normalize(rotA * (xDir * meter));
        const vA   = cross(nDir, uA);
        const rotB = rotationAround(line(vector(0, 0, 0) * meter, nDir), -definition.angle);
        const uB   = normalize(rotB * (xDir * meter));
        const vB   = cross(nDir, uB);
        const count = ceil(reach / cs);

        const la = (tw / 2) / sin(2 * definition.angle) + hb / 2; // half the distance from the lattice center to the hole edge along the diagonal, + margin for the hole border
        const lb = la; // same in this case, but could be different if the angle is different for the two families

        const chordExt = min(extX, extY);            // extent along chordDir (the shorter in-plane axis)
            const spanExt  = max(extX, extY);            // extent along spanDir  (the longer  in-plane axis)
            const hiC = chordExt / 2 + cs;    // extents from center plus 1 cell
            const hiS = spanExt  / 2 + cs;
            var crossings = [];
            for (var k = -count; k <= count; k += 1) // for each A slab
            {
                const pA = center + vA * (k * cs); // finds the center of the A slab (center + offset direction* slab number*slab spacing)
                for (var j = -count; j <= count; j += 1) // for each A slab
                {
                    const pB = center + vB * (j * cs); // finds the center of the B slab (center + offset direction* slab number*slab spacing)
                    const t  = lineCrossT(pA, uA, pB, uB, xDir, yDir); // finds the intersection
                    if (abs(t) <= reach) // checks if it's the slab or the vector that intersects
                    {
                        const crossing = pA + uA * t; // finds the intersection point
                        const cx = dot(crossing - center, chordDir);
                        const cy = dot(crossing - center, spanDir);
                        if (abs(cx) <= hiC && abs(cy) <= hiS) // checks if intersection is in the wing
                        {
                            crossings = append(crossings,
                                columnMidpoint(context, definition.wing, crossing, nDir, extZ / 2 + cs)); // finds collumn midpoint
                        }
                    }
                }
            }
            var hasKeep = size(crossings) > 0;
            var pillarKeep = qUnion([]);
            if (hasKeep)
            {
                const reachN = height / 2 + cs;
                //   la is passed for the curve radius too (3rd slot) -- the rounding stays proportional
                //   to the waist, same as before.
                const seed = makeHourglassPrism(context, id + "keepSeed", crossings[0],
                                                uA, uB, vA, vB, nDir, la, lb, la, reachN, reachN);
                var xforms = [];
                var names  = [];
                for (var i = 1; i < size(crossings); i += 1)
                {
                    xforms = append(xforms, transform(identityMatrix(3), crossings[i] - crossings[0]));
                    names  = append(names, "k" ~ toString(i));
                }
                if (size(xforms) > 0)
                {
                    opPattern(context, id + "keepPat", {
                        "entities" : seed,
                        "transforms" : xforms,
                        "instanceNames" : names
                    });
                    pillarKeep = qUnion([seed, qCreatedBy(id + "keepPat", EntityType.BODY)]);
                }
            }

            // make protective shells around the lattice to keep the holes from cutting into the wing surface
            const hbN = [nDir * hb, -(nDir * hb),
                         normalize(chordDir + nDir) * hb, normalize(chordDir - nDir) * hb,
                         normalize(-chordDir + nDir) * hb, normalize(-chordDir - nDir) * hb];
            var shellLayers = [];
            for (var i = 0; i < size(hbN); i += 1)
            {
                const lyr = dup(context, id + ("hShell" ~ toString(i)), definition.wing);
                const cz  = dup(context, id + ("hCut"   ~ toString(i)), definition.wing);
                opTransform(context, id + ("hCutXf" ~ toString(i)), {
                    "bodies" : cz, "transform" : transform(identityMatrix(3), hbN[i])
                });
                opBoolean(context, id + ("hLayer" ~ toString(i)), {
                    "targets" : lyr, "tools" : cz, "operationType" : BooleanOperationType.SUBTRACTION
                });
                shellLayers = append(shellLayers, lyr);
            }
            //merge the shell layers into two halves, then union them together to make a single shell
            const hbHalf = floor(size(shellLayers) / 2);
            var hbFirst = [];
            var hbSecond = [];
            for (var i = 0; i < size(shellLayers); i += 1)
            {
                if (i < hbHalf) { hbFirst = append(hbFirst, shellLayers[i]); }
                else { hbSecond = append(hbSecond, shellLayers[i]); }
            }
            opBoolean(context, id + "hShellU", {
                "targets" : qUnion(hbFirst),
                "tools"   : qUnion(hbSecond),
                "operationType" : BooleanOperationType.UNION,
                "targetsAndToolsNeedGrouping" : true
            });
            const shellU = qUnion(shellLayers);

            var confineTools = shellU;
            if (hasKeep) { confineTools = qUnion([shellU, pillarKeep]); }

            opBoolean(context, id + "confine", { // trims the web
                "targets" : qUnion([cutA, cutB]), "tools" : confineTools,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
            // lattice cuts
            opBoolean(context, id + "punchA", { 
                "targets" : familyA, "tools" : cutA, "operationType" : BooleanOperationType.SUBTRACTION
            });
            opBoolean(context, id + "punchB", {
                "targets" : familyB, "tools" : cutB, "operationType" : BooleanOperationType.SUBTRACTION
            });
    }

    opBoolean(context, id + "merge", {
            "targets" : familyA,
            "tools"   : familyB,
            "operationType" : BooleanOperationType.UNION,
            "targetsAndToolsNeedGrouping" : true
    });
    const lattice = qUnion([familyA, familyB]); // once again just for ease of use

    const lbox = evBox3d(context, { "topology" : lattice, "tight" : false });
    const pad  = vector(1, 1, 1) * definition.cellSize;
    fCuboid(context, id + "bbox", {
        "corner1" : lbox.minCorner - pad,
        "corner2" : lbox.maxCorner + pad
    });
    opBoolean(context, id + "neg", { //substracts the wing from the box
        "targets" : qCreatedBy(id + "bbox", EntityType.BODY),
        "tools"   : dup(context, id + "wingNeg", definition.wing),
        "operationType" : BooleanOperationType.SUBTRACTION
    });
    opBoolean(context, id + "trim", { // trims the lattice to the wing shape (lattice - (box - wing))
        "targets" : lattice,
        "tools"   : qCreatedBy(id + "bbox", EntityType.BODY),
        "operationType" : BooleanOperationType.SUBTRACTION
    });


    const t = definition.skinThickness;
    const nudges = [chordDir * t, -(chordDir * t), nDir * t, -(nDir * t)];
    var layers = [];
    for (var i = 0; i < size(nudges); i += 1)
    {
        const layer = dup(context, id + ("skin" ~ toString(i)), definition.wing);
        const cut   = dup(context, id + ("cut" ~ toString(i)), definition.wing);
        opTransform(context, id + ("cutXf" ~ toString(i)), {
            "bodies" : cut,
            "transform" : transform(identityMatrix(3), nudges[i])
        });
        opBoolean(context, id + ("layer" ~ toString(i)), {
            "targets" : layer,
            "tools"   : cut,
            "operationType" : BooleanOperationType.SUBTRACTION
        });
        layers = append(layers, layer);
    }

    if (definition.capTip || definition.capRoot)
    {
        const wtight = evBox3d(context, { "topology" : definition.wing,
                                            "cSys" : coordSystem(center, xDir, nDir), "tight" : true });
        var tipPlane = wtight.maxCorner[spanIdx] - t;
        var tipKeep  = 1;
        var rootPlane = wtight.minCorner[spanIdx] + t;
        var rootKeep  = -1;
        if (definition.capTip)
        {
            layers = append(layers, makeSpanCap(context, id + "tipCap", definition.wing, center,
                                                chordDir, spanDir, nDir, tipPlane, tipKeep, reach, height));
        }
        if (definition.capRoot)
        {
            layers = append(layers, makeSpanCap(context, id + "rootCap", definition.wing, center,
                                                chordDir, spanDir, nDir, rootPlane, rootKeep, reach, height));
        }
    }

    opBoolean(context, id + "join", {
        "targets" : lattice,
        "tools"   : qUnion(layers),
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true
    });

    // optionally drop the input wing
    if (!definition.keepWing)
    {
        opDeleteBodies(context, id + "delWing", { "entities" : definition.wing });
    }
});

function makeFamily(context is Context, id is Id, tag is string, ang is ValueWithUnits,
                    center is Vector, xDir is Vector, nDir is Vector,
                    reach is ValueWithUnits, height is ValueWithUnits,
                    cs is ValueWithUnits, t is ValueWithUnits) returns Query
{
    
    const rot = rotationAround(line(vector(0, 0, 0) * meter, nDir), ang);
    const u   = normalize(rot * (xDir * meter)); // rotates the x axis to the desired angle
    const v   = cross(nDir, u); // the direction perpendicular to the rotated x axis and the plane normal (in-plane diagonal)
    const count = ceil(reach / cs);

    const sid = id + ("slab" ~ tag ~ "seed"); // seed id
    fCuboid(context, sid, { // make the seed slab
        "corner1" : vector(-reach, -t / 2, -height / 2),
        "corner2" : vector( reach,  t / 2,  height / 2)
    });
    opTransform(context, id + ("xf" ~ tag ~ "seed"), { // move the seed slab to the center of the wing
        "bodies" : qCreatedBy(sid, EntityType.BODY),
        "transform" : toWorld(coordSystem(center, u, nDir))
    });
    var xforms = [];  //copies' transforms (positions)
    var names  = []; // copies' names
    for (var k = -count; k <= count; k += 1)
    {
        if (k == 0) { continue; }
        xforms = append(xforms, transform(identityMatrix(3), v * (k * cs)));
        names  = append(names, "s" ~ toString(k));
    }
    if (size(xforms) > 0)
    {
        opPattern(context, id + ("pat" ~ tag), { // actually make the copies
            "entities" : qCreatedBy(sid, EntityType.BODY),
            "transforms" : xforms,
            "instanceNames" : names
        });
        return qUnion([qCreatedBy(sid, EntityType.BODY), qCreatedBy(id + ("pat" ~ tag), EntityType.BODY)]); // returns a single list 
    }
    return qCreatedBy(sid, EntityType.BODY); // fallback if no pattern was created (count=0)
}
function makeSpanCap(context is Context, id is Id, wing is Query, center is Vector,
                     chordDir is Vector, spanDir is Vector, nDir is Vector,
                     planeSpan is ValueWithUnits, keepDir is number,
                     reach is ValueWithUnits, height is ValueWithUnits) returns Query
{
    const capId = id + "cap";
    dup(context, capId, wing);                             // wing copy to carve the cap from
    //   Removal block: local x->chord, y->span, z->thickness. Symmetric +/-reach along span and offset
    //   inboard by keepDir*reach, so its near face lands exactly on the cut plane and it extends 2*reach
    //   toward the interior. Oversized in chord/thickness so it spans the whole section.
    const cutBoxId = id + "capBox";
    fCuboid(context, cutBoxId, {
        "corner1" : vector(-reach, -reach, -height),
        "corner2" : vector( reach,  reach,  height)
    });
    opTransform(context, id + "capBoxXf", {
        "bodies" : qCreatedBy(cutBoxId, EntityType.BODY),
        "transform" : toWorld(coordSystem(center + spanDir * (planeSpan - keepDir * reach), chordDir, nDir))
    });
    opBoolean(context, id + "capCut", {
        "targets" : qCreatedBy(capId, EntityType.BODY),
        "tools"   : qCreatedBy(cutBoxId, EntityType.BODY),
        "operationType" : BooleanOperationType.SUBTRACTION
    });
    return qCreatedBy(capId, EntityType.BODY);
}

function lineCrossT(p1 is Vector, d1 is Vector, p2 is Vector, d2 is Vector,
                    xDir is Vector, yDir is Vector) returns ValueWithUnits
{
    // find x, y coords of the two points and two direction vectors
    const p1x = dot(p1, xDir); const p1y = dot(p1, yDir);
    const p2x = dot(p2, xDir); const p2y = dot(p2, yDir);
    const d1x = dot(d1, xDir); const d1y = dot(d1, yDir);
    const d2x = dot(d2, xDir); const d2y = dot(d2, yDir);

    // solve for t in the equation p1 + d1 * t = p2 + d2 * s, where s is a parameter for the second line
    const denom = d1x * d2y - d1y * d2x;
    return ((p2x - p1x) * d2y - (p2y - p1y) * d2x) / denom;
}

// cut everything from a plane
function makeHalfSpaceCutter(context is Context, id is Id, planePoint is Vector,
                             outDir is Vector, nDir is Vector,
                             big is ValueWithUnits, height is ValueWithUnits) returns Query
{
    const cId = id + "c";
    fCuboid(context, cId, {
        "corner1" : vector(0 * meter, -big, -height / 2),
        "corner2" : vector(big,        big,  height / 2)
    });
    opTransform(context, id + "cXf", {
        "bodies" : qCreatedBy(cId, EntityType.BODY),
        "transform" : toWorld(coordSystem(planePoint, outDir, nDir))
    });
    return qCreatedBy(cId, EntityType.BODY);
}

function makeCurvedFlareCutter(context is Context, id is Id, waist is Vector,
                               u is Vector, axisDir is Vector, v is Vector,
                               l is ValueWithUnits, r is ValueWithUnits, big is ValueWithUnits) returns Query
{
    const zt   = r / sqrt(2);                  // height where the arc's slope reaches 45 deg
    const tipU = l + r * (1 - 1 / sqrt(2));     // u-coordinate of that same tangent point

    // Near piece (0 <= z <= zt): "inside the rounding cylinder" union "u > l+r" together equal
    // exactly "u beyond the circle's left edge, out to +big" -- the same half-space trick
    // makeHalfSpaceCutter uses for a flat boundary, just with a curved one.
    const cylId = id + "cyl";
    fCylinder(context, cylId, {
        "topCenter"    : waist + u * (l + r) + v * big,
        "bottomCenter" : waist + u * (l + r) - v * big,
        "radius"       : r
    });
    const farSide = makeHalfSpaceCutter(context, id + "fs", waist + u * (l + r), u, v, big, big);
    opBoolean(context, id + "nearU", {
        "targets" : qCreatedBy(cylId, EntityType.BODY),
        "tools"   : farSide,
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true
    });
    const nearRaw = qUnion([qCreatedBy(cylId, EntityType.BODY), farSide]);
    const nearCap = makeHalfSpaceCutter(context, id + "nCap", waist + axisDir * zt, axisDir, u, big, big);
    opBoolean(context, id + "nearCut", {
        "targets" : nearRaw, "tools" : nearCap, "operationType" : BooleanOperationType.SUBTRACTION
    });

    // Far piece (z >= zt): the same flat 45-deg plane the old, un-rounded design used everywhere,
    // anchored at the arc's tangent point instead of the waist.
    const dFar    = normalize(u - axisDir);
    const farFlat = makeHalfSpaceCutter(context, id + "ff", waist + u * tipU + axisDir * zt, dFar, v, big, big);
    const farCap  = makeHalfSpaceCutter(context, id + "fCap", waist + axisDir * zt, -axisDir, u, big, big);
    opBoolean(context, id + "farCut", {
        "targets" : farFlat, "tools" : farCap, "operationType" : BooleanOperationType.SUBTRACTION
    });

    // nearRaw and farFlat share the z=zt plane exactly (face contact, not overlap) -- the grouped-
    // union idiom, same reason the hourglass halves and the slab merge need it.
    opBoolean(context, id + "join", {
        "targets" : nearRaw,
        "tools"   : farFlat,
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true
    });
    return qUnion([nearRaw, farFlat]);
}

function makeFrustumHalf(context is Context, id is Id, waist is Vector,
                         uA is Vector, uB is Vector, vA is Vector, vB is Vector, axisDir is Vector,
                         la is ValueWithUnits, lb is ValueWithUnits, r is ValueWithUnits,
                         reach is ValueWithUnits, big is ValueWithUnits) returns Query
{
    const baseId = id + "base";
    fCuboid(context, baseId, {
        "corner1" : vector(-big, -big, -big),
        "corner2" : vector( big,  big,  big)
    });
    opTransform(context, id + "baseXf", {
        "bodies" : qCreatedBy(baseId, EntityType.BODY),
        "transform" : toWorld(coordSystem(waist, uA, axisDir))
    });

    const cutA1 = makeCurvedFlareCutter(context, id + "cA1", waist,  uA, axisDir, vA, la, r, big);
    const cutA2 = makeCurvedFlareCutter(context, id + "cA2", waist, -uA, axisDir, vA, la, r, big);
    const cutB1 = makeCurvedFlareCutter(context, id + "cB1", waist,  uB, axisDir, vB, lb, r, big);
    const cutB2 = makeCurvedFlareCutter(context, id + "cB2", waist, -uB, axisDir, vB, lb, r, big);
    const cutZ0 = makeHalfSpaceCutter(context, id + "cZ0", waist,                  -axisDir, uA, big, big);
    const cutZ1 = makeHalfSpaceCutter(context, id + "cZ1", waist + axisDir * reach, axisDir, uA, big, big);

    opBoolean(context, id + "carve", {
        "targets" : qCreatedBy(baseId, EntityType.BODY),
        "tools"   : qUnion([cutA1, cutA2, cutB1, cutB2, cutZ0, cutZ1]),
        "operationType" : BooleanOperationType.SUBTRACTION
    });

    const axisPt  = waist + axisDir * (reach / 2); // center point
    const central = qContainsPoint(qCreatedBy(baseId, EntityType.BODY), axisPt); // part containing center point
    const slivers = qSubtraction(qCreatedBy(baseId, EntityType.BODY), central); // delete everything else
    if (size(evaluateQuery(context, slivers)) > 0) // make sure
    {
        opDeleteBodies(context, id + "trimCorners", { "entities" : slivers });
    }
    return qCreatedBy(baseId, EntityType.BODY);
}

function makeHourglassPrism(context is Context, id is Id, waist is Vector,
                            uA is Vector, uB is Vector, vA is Vector, vB is Vector, nDir is Vector,
                            la is ValueWithUnits, lb is ValueWithUnits, r is ValueWithUnits,
                            topReach is ValueWithUnits, bottomReach is ValueWithUnits) returns Query
{
    const big = 4 * (la + lb + topReach + bottomReach); // big number to make sure cutter works
    const upper = makeFrustumHalf(context, id + "U", waist, uA, uB, vA, vB,  nDir, la, lb, r, topReach,    big);
    const lower = makeFrustumHalf(context, id + "D", waist, uA, uB, vA, vB, -nDir, la, lb, r, bottomReach, big);

    opBoolean(context, id + "hgU", {
        "targets" : upper,
        "tools"   : lower,
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true
    });
    return qUnion([upper, lower]);
}

function columnMidpoint(context is Context, wing is Query, samplePoint is Vector,
                        nDir is Vector, reachAlongN is ValueWithUnits) returns Vector
{
    const hits = evRaycast(context, {
        "entities" : wing,
        "ray"      : line(samplePoint - nDir * reachAlongN, nDir) // from below the wing, along the normal
    });
    if (size(hits) < 2)
    {
        return samplePoint;
    }
    const p0 = hits[0].intersection;
    const p1 = hits[size(hits) - 1].intersection;
    if (p0 == undefined || p1 == undefined)
    {
        return samplePoint;
    }
    return (p0 + p1) / 2;
}

// Copy a body so the original survives a later consuming boolean.
// opPattern with one identity transform is the reliable copy idiom; opTransform + makeCopy
// on an identity transform does not register a queryable body.
function dup(context is Context, id is Id, body is Query) returns Query
{
    opPattern(context, id, {
        "entities"      : body,
        "transforms"    : [identityTransform()],
        "instanceNames" : ["1"]
    });
    return qCreatedBy(id, EntityType.BODY);
}