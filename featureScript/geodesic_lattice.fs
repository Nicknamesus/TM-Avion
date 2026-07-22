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

    annotation { "Name" : "Flip tip/root to other span side", "Default" : false }
    definition.flipTip is boolean;
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
    const extX   = wbox.maxCorner[0] - wbox.minCorner[0];   // spanwise extent
    const extY   = wbox.maxCorner[1] - wbox.minCorner[1];   // chordwise extent
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