FeatureScript 3008;
import(path : "onshape/std/common.fs", version : "3008.0");

export enum KnuckleChamferEnd // new type for dropdown
{
    annotation { "Name" : "-pinAxis end (down in the standard print)" }
    MINUS,
    annotation { "Name" : "+pinAxis end (for a part printed the other way up)" }
    PLUS,
    annotation { "Name" : "Both ends (symmetric -- prints either way up)" }
    BOTH
}

annotation { "Feature Type Name" : "Control surface" }
export const controlSurface = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Surface shell(s)", "Filter" : EntityType.BODY && BodyType.SOLID }
        definition.bodies is Query;

        annotation { "Name" : "Hinge plane", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.hingePlane is Query;

        annotation { "Name" : "Movable strip is on +normal side of hinge plane (uncheck to flip)", "Default" : true }
        definition.movablePlusNormal is boolean;

        annotation { "Name" : "Auto-detect hinge axis (uncheck to pick it)", "Default" : true }
        definition.autoAxis is boolean;
        if (!definition.autoAxis)
        {
            annotation { "Name" : "Hinge-axis reference (plane normal = spanwise)", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
            definition.axisRef is Query;
        }

        annotation { "Name" : "Use separate solid reference (pre-lattice loft) -- REQUIRED for latticed shells", "Default" : false }
        definition.useSolidRef is boolean;
        if (definition.useSolidRef)
        {
            annotation { "Name" : "Solid reference (pre-lattice loft)", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
            definition.solidRef is Query;
        }

        annotation { "Name" : "Inboard bound", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.inboardPlane is Query;
        annotation { "Name" : "Outboard bound", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.outboardPlane is Query;

        annotation { "Name" : "Running gap (fixed<->movable)" }
        isLength(definition.runningGap, { (millimeter) : [0.2, 0.8, 3] } as LengthBoundSpec);

        annotation { "Name" : "Closing wall thickness (min; auto-raised to knuckle radius for anchoring)" }
        isLength(definition.ribThickness, { (millimeter) : [0.6, 1.5, 5] } as LengthBoundSpec);

        annotation { "Name" : "Skin thickness (end walls -- match the shell skin)" }
        isLength(definition.skinThickness, { (millimeter) : [0.2, 0.42, 3] } as LengthBoundSpec);

        annotation { "Name" : "Movable LE bevel half-angle" }
        isAngle(definition.leBevel, { (degree) : [0, 30, 60] } as AngleBoundSpec);

        annotation { "Name" : "Knuckle datum", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.knuckleDatum is Query;

        annotation { "Name" : "Knuckle pitch (center to center)" }
        isLength(definition.knucklePitch, { (millimeter) : [6, 26, 80] } as LengthBoundSpec);

        annotation { "Name" : "Knuckle length (barrel height)" }
        isLength(definition.knuckleLength, { (millimeter) : [3, 16, 60] } as LengthBoundSpec);

        annotation { "Name" : "Knuckle radius" }
        isLength(definition.knuckleRadius, { (millimeter) : [1.5, 3, 8] } as LengthBoundSpec);

        annotation { "Name" : "Knuckle chamfer end (self-supporting teardrop)" }
        definition.knuckleChamfer is KnuckleChamferEnd;

        annotation { "Name" : "Pin diameter (music wire)" }
        isLength(definition.pinDiameter, { (millimeter) : [0.8, 1.2, 4] } as LengthBoundSpec);

        annotation { "Name" : "Pin bore clearance (over wire radius)" }
        isLength(definition.boreClearance, { (millimeter) : [0.05, 0.2, 1] } as LengthBoundSpec);

        annotation { "Name" : "Hinge running clearance (knuckle<->foreign socket)" }
        isLength(definition.fitClearance, { (millimeter) : [0.1, 0.3, 1.5] } as LengthBoundSpec);

        annotation { "Name" : "Pin insertion channel to wing end", "Default" : true }
        definition.pinChannel is boolean;
        if (definition.pinChannel)
        {
            annotation { "Name" : "Insert from outboard end (uncheck for inboard)", "Default" : true }
            definition.pinChannelOutboard is boolean;
        }

        annotation { "Name" : "Keep original shell(s) (moved aside for reference)", "Default" : false }
        definition.keepOriginal is boolean;
        if (definition.keepOriginal)
        {
            annotation { "Name" : "Keep-original offset (perpendicular to surface)" }
            isLength(definition.keepOffset, { (millimeter) : [10, 150, 600] } as LengthBoundSpec);
        }

        annotation { "Name" : "Add horn on movable surface", "Default" : false }
        definition.addHorn is boolean;
        if (definition.addHorn)
        {
            annotation { "Name" : "Horn station (plane origin sets the span position)", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
            definition.hornPlane is Query;

            annotation { "Name" : "Horn on +thickness side (uncheck for the other face)", "Default" : false }
            definition.hornPlusThickness is boolean;

            annotation { "Name" : "Horn height (skin surface -> hole centre)" }
            isLength(definition.hornHeight, { (millimeter) : [1, 7, 40] } as LengthBoundSpec);

            annotation { "Name" : "Horn blade thickness (spanwise)" }
            isLength(definition.hornThickness, { (millimeter) : [0.8, 2, 8] } as LengthBoundSpec);

            annotation { "Name" : "Horn root chord (blade/rib length at the skin)" }
            isLength(definition.hornChord, { (millimeter) : [4, 12, 40] } as LengthBoundSpec);

            annotation { "Name" : "Horn hole diameter (Z-bend / clevis / ball link)" }
            isLength(definition.hornBore, { (millimeter) : [0.8, 1.6, 4] } as LengthBoundSpec);

            annotation { "Name" : "Horn-to-wing clearance (throw gap)" }
            isLength(definition.hornClearance, { (millimeter) : [0.2, 1, 5] } as LengthBoundSpec);

            annotation { "Name" : "45deg print gusset (root-side web -- support-free, but wide)", "Default" : false }
            definition.hornGusset is boolean;
            if (definition.hornGusset)
            {
                annotation { "Name" : "Gusset on +pinAxis side (only if the part prints the other way up)", "Default" : false }
                definition.hornGussetFlip is boolean;
            }
        }

        annotation { "Name" : "Add servo bay in the fixed surface", "Default" : false }
        definition.addServoBay is boolean;
        if (definition.addServoBay)
        {
            annotation { "Name" : "Servo station (plane origin sets the span position)", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
            definition.servoPlane is Query;

            // the mounting ears seat on it without rocking.
            annotation { "Name" : "Servo on +thickness side (uncheck for the lower skin)", "Default" : false }
            definition.servoPlusThickness is boolean;

            annotation { "Name" : "Bay forward offset (hinge axis -> case centre)" }
            isLength(definition.servoOffset, { (millimeter) : [5, 25, 150] } as LengthBoundSpec);

            annotation { "Name" : "Bay sideways offset (along hinge axis, +/-)" }
            isLength(definition.servoSpanOffset, { (millimeter) : [-100, 0, 100] } as LengthBoundSpec);

            annotation { "Name" : "Case length (HS-40: 20 mm)" }
            isLength(definition.servoLength, { (millimeter) : [5, 20, 60] } as LengthBoundSpec);

            annotation { "Name" : "Case width (HS-40: 8.7 mm)" }
            isLength(definition.servoWidth, { (millimeter) : [3, 8.7, 40] } as LengthBoundSpec);

            annotation { "Name" : "Case depth INTO the wing (HS-40: 12 of its 16.5 mm)" }
            isLength(definition.servoDepth, { (millimeter) : [3, 12, 40] } as LengthBoundSpec);

            annotation { "Name" : "Case length runs spanwise (uncheck for chordwise)", "Default" : true }
            definition.servoSpanwise is boolean;

            annotation { "Name" : "Pocket clearance (per side)" }
            isLength(definition.servoClearance, { (millimeter) : [0, 0.3, 2] } as LengthBoundSpec);

            annotation { "Name" : "Screw hole diameter" }
            isLength(definition.servoScrewBore, { (millimeter) : [0.8, 2, 6] } as LengthBoundSpec);

            annotation { "Name" : "Screw hole spacing (ear to ear -- MEASURE IT, not published)" }
            isLength(definition.servoScrewSpacing, { (millimeter) : [8, 26, 70] } as LengthBoundSpec);

            annotation { "Name" : "Solid collar depth under the skin (screw bite; >= case depth walls the pocket fully)" }
            isLength(definition.servoCollar, { (millimeter) : [1, 12, 40] } as LengthBoundSpec);
        }
    }
    {
        const hp   = evPlane(context, { "face" : definition.hingePlane });
        var cutN   = hp.normal;
        if (!definition.movablePlusNormal) { cutN = -cutN; } // cutN now points toward the movable (aft) side

        const aabb   = evBox3d(context, { "topology" : definition.bodies, "tight" : false });
        const center = (aabb.minCorner + aabb.maxCorner) / 2; //same logic as in lattice

        //directions
        const a1 = hp.x;
        const a2 = normalize(cross(cutN, a1));
        var pinAxis  = a1;
        var thickDir = a2;

        if (definition.autoAxis)
        {
            const inBox = evBox3d(context, {
                "topology" : definition.bodies,
                "cSys"     : coordSystem(center, a1, a2),
                "tight"    : true // to avoid rotation errors
            });

            const ext1 = inBox.maxCorner[0] - inBox.minCorner[0];
            const ext2 = inBox.maxCorner[2] - inBox.minCorner[2];

            if (ext2 > ext1) { pinAxis = a2; thickDir = -a1; }
        } else
        {
            const ap   = evPlane(context, { "face" : definition.axisRef });
            const proj = ap.normal - cutN * dot(ap.normal, cutN); // remove hinge-plane parallel par of verctor (math is v - (v.n)n, (v.n) giving a scalar of v on n and *n giving a vector)
            if (norm(proj) < 1e-5) // unit normals are dimensionless
            {
                throw regenError("Hinge-axis reference normal is parallel to the hinge-plane normal; "
                    ~ "pick a plane whose normal points along the span.", ["axisRef"]);
            }
            pinAxis  = normalize(proj);
            thickDir = normalize(cross(cutN, pinAxis));
        }

        // Scalar coordinate of a point along a direction.
        const along = function(pt is Vector, dir is Vector) returns ValueWithUnits { return dot(pt - center, dir); };

        const shellBox = evBox3d(context, {
            "topology" : definition.bodies,
            "cSys"     : coordSystem(center, pinAxis, cutN),
            "tight"    : true
        });
        const spanExtent  = shellBox.maxCorner[0] - shellBox.minCorner[0];
        const chordExtent = shellBox.maxCorner[2] - shellBox.minCorner[2];
        const big = spanExtent + chordExtent + 2 * definition.knuckleRadius + 50 * millimeter; //oversized

        const hingePos = along(hp.origin, cutN);    // hinge location along the cut axis
        const gap      = definition.runningGap;

        const cutLoShell = shellBox.minCorner[2];                         // min for cutN (CHORD)
        const cutHiShell = shellBox.maxCorner[2];                       
        const cutExt     = cutHiShell - cutLoShell;                       // along cutN (CHORD)
        const thickExt   = shellBox.maxCorner[1] - shellBox.minCorner[1]; // along cutN x pinAxis (thickness)

        // checks
        const eMax = max(spanExtent, max(cutExt, thickExt));
        const eMin = min(spanExtent, min(cutExt, thickExt));
        const eMid = spanExtent + cutExt + thickExt - eMax - eMin;
        if (cutExt <= eMin)
        {
            throw regenError("Hinge plane normal points along the WING THICKNESS (the shell is only "
                ~ toString(roundToPrecision(cutExt / millimeter, 1)) ~ " mm along it) -- the plane is "
                ~ "lying flat. Rotate it 90deg so it stands vertically and its normal points CHORDWISE "
                ~ "(LE->TE); the hinge line should run spanwise and cut the airfoil top-to-bottom.",
                ["hingePlane"]);
        }
        if (cutExt >= eMax && eMax > 1.5 * eMid)
        {
            throw regenError("Hinge plane normal points along the SPAN (the shell is "
                ~ toString(roundToPrecision(cutExt / millimeter, 1)) ~ " mm along it -- its longest axis). "
                ~ "This plane slices the wing into inboard/outboard halves, like a rib cross-section, "
                ~ "not a trailing-edge flap. Rotate it 90deg (about the vertical) so it runs SPANWISE at "
                ~ "the ~70% chord line, with its normal pointing chordwise (LE->TE) -- perpendicular to "
                ~ "your inboard/outboard trim planes.", ["hingePlane"]);
        }
        if (hingePos <= cutLoShell || hingePos >= cutHiShell)
        {
            throw regenError("Hinge plane does not cross the shell -- it is positioned off the chord "
                ~ "(hingePos=" ~ toString(roundToPrecision(hingePos / millimeter, 1)) ~ " mm vs chord "
                ~ "range [" ~ toString(roundToPrecision(cutLoShell / millimeter, 1)) ~ ", "
                ~ toString(roundToPrecision(cutHiShell / millimeter, 1)) ~ "] mm). Offset it to the "
                ~ "hinge station (e.g. 70% chord).", ["hingePlane"]);
        }

        const hingeAxisPos = hingePos + gap / 2; 

        const wallDepth = max(definition.ribThickness, definition.knuckleRadius + definition.fitClearance); //hinge attachment wall

        // end and start with planes
        var spanLo = shellBox.minCorner[0];
        var spanHi = shellBox.maxCorner[0];
        const inP  = evPlane(context, { "face" : definition.inboardPlane });
        const outP = evPlane(context, { "face" : definition.outboardPlane });
        const s0 = along(inP.origin, pinAxis);
        const s1 = along(outP.origin, pinAxis);
        spanLo = min(s0, s1);
        spanHi = max(s0, s1);

        var solidRef = definition.bodies; // either solid or a + loft
        if (definition.useSolidRef)
        {
            solidRef = csDup(context, id + "solidRefDup", definition.solidRef);
        }

        const bLo = shellBox.minCorner[0];
        const bHi = shellBox.maxCorner[0];
        if (bHi <= spanLo || bLo >= spanHi)
        {
            throw regenError("The picked body does not overlap the control-surface span range -- "
                ~ "check the hinge plane / span bounds against the shell.");
        }

        const pLo = max(bLo, spanLo);
        const pHi = min(bHi, spanHi);
        const pid = id + "p";

        //cut out movable
        const movable = csDup(context, pid + "mvDup", definition.bodies);
        opBoolean(context, pid + "mvFwd", { // drop everything fwd of hinge+gap
            "targets" : movable,
            "tools"   : csHalfSpace(context, pid + "mvFwdHS", center + cutN * (hingePos + gap), -cutN, pinAxis, big),
            "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, pid + "mvLo", {
            "targets" : movable,
            "tools"   : csHalfSpace(context, pid + "mvLoHS", center + pinAxis * pLo, -pinAxis, cutN, big),
            "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, pid + "mvHi", {
            "targets" : movable,
            "tools"   : csHalfSpace(context, pid + "mvHiHS", center + pinAxis * pHi, pinAxis, cutN, big),
            "operationType" : BooleanOperationType.SUBTRACTION });


        // cutout cs space with box
        const region = csOrientedBox(context, pid + "region", center, cutN, pinAxis, thickDir,
                                     hingePos, hingePos + big, pLo, pHi, -big, big);
        const fixed = csDup(context, pid + "fxDup", definition.bodies);
        opBoolean(context, pid + "fxSub", {
            "targets" : fixed, "tools" : region,
            "operationType" : BooleanOperationType.SUBTRACTION });    

        
         if (definition.keepOriginal)
        {
            opTransform(context, pid + "keepXf", {
                "bodies"    : definition.bodies,
                "transform" : transform(identityMatrix(3), thickDir * definition.keepOffset)
            });
        }
        else
        {
            opDeleteBodies(context, pid + "delOrig", { "entities" : definition.bodies });
        }


        // walls facing hinge
        const coveRib = csSectionSlab(context, pid + "cove", solidRef,
            hingePos - wallDepth, hingePos, pLo, pHi,
            cutN, pinAxis, center, big);
        opBoolean(context, pid + "coveU", {
        "targets" : fixed, "tools" : coveRib,
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true });

        const leRib = csSectionSlab(context, pid + "le", solidRef,
            hingePos + gap, hingePos + gap + wallDepth, pLo, pHi,
            cutN, pinAxis, center, big);
        opBoolean(context, pid + "leU", {
        "targets" : movable, "tools" : leRib,
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true });

        // start-end walls
        const endTol = 0.05 * millimeter;
        const skinT  = definition.skinThickness;
        if (pLo - spanLo < endTol) // inboard end
        {
            const mvEnd = csSectionSlab(context, pid + "endMvLo", solidRef,
                                        hingePos + gap, hingePos + big, pLo, pLo + skinT,
                                        cutN, pinAxis, center, big);
            opBoolean(context, pid + "endMvLoU", {
                "targets" : movable, "tools" : mvEnd,
                "operationType" : BooleanOperationType.UNION, "targetsAndToolsNeedGrouping" : true });
            const fxEnd = csSectionSlab(context, pid + "endFxLo", solidRef,
                                        hingePos - wallDepth, hingePos + big, pLo - skinT, pLo,
                                        cutN, pinAxis, center, big);
            opBoolean(context, pid + "endFxLoU", {
                "targets" : fixed, "tools" : fxEnd,
                "operationType" : BooleanOperationType.UNION, "targetsAndToolsNeedGrouping" : true });
        }
        if (spanHi - pHi < endTol) // outboard end
        {
            const mvEnd = csSectionSlab(context, pid + "endMvHi", solidRef,
                                        hingePos + gap, hingePos + big, pHi - skinT, pHi,
                                        cutN, pinAxis, center, big);
            opBoolean(context, pid + "endMvHiU", {
                "targets" : movable, "tools" : mvEnd,
                "operationType" : BooleanOperationType.UNION, "targetsAndToolsNeedGrouping" : true });
            const fxEnd = csSectionSlab(context, pid + "endFxHi", solidRef,
                                        hingePos - wallDepth, hingePos + big, pHi, pHi + skinT,
                                        cutN, pinAxis, center, big);
            opBoolean(context, pid + "endFxHiU", {
                "targets" : fixed, "tools" : fxEnd,
                "operationType" : BooleanOperationType.UNION, "targetsAndToolsNeedGrouping" : true });
        }

        if (definition.leBevel > 0 * degree)
        {

            const pinPt = center + cutN * hingeAxisPos; // on the pin axis (thickness-centered)
            // get cut unit vectors
            const b1 = normalize(-cutN * cos(definition.leBevel) + thickDir * sin(definition.leBevel));
            const b2 = normalize(-cutN * cos(definition.leBevel) - thickDir * sin(definition.leBevel));
            opBoolean(context, pid + "bev1", {
                "targets" : movable, "tools" : csHalfSpace(context, pid + "bev1HS", pinPt, b1, pinAxis, big),
                "operationType" : BooleanOperationType.SUBTRACTION });
            opBoolean(context, pid + "bev2", {
                "targets" : movable, "tools" : csHalfSpace(context, pid + "bev2HS", pinPt, b2, pinAxis, big),
                "operationType" : BooleanOperationType.SUBTRACTION });


            const bevelReach = (thickExt / 2) * tan(definition.leBevel);
            const sealAft    = hingeAxisPos + bevelReach + wallDepth;
            // le slab
            const leSeal = csSectionSlab(context, pid + "leSeal", solidRef,
                                         hingePos + gap, sealAft, pLo, pHi,
                                         cutN, pinAxis, center, big);
            // cut bevels then usion
            opBoolean(context, pid + "leSealB1", {
                "targets" : leSeal, "tools" : csHalfSpace(context, pid + "leSealB1HS", pinPt, b1, pinAxis, big),
                "operationType" : BooleanOperationType.SUBTRACTION });
            opBoolean(context, pid + "leSealB2", {
                "targets" : leSeal, "tools" : csHalfSpace(context, pid + "leSealB2HS", pinPt, b2, pinAxis, big),
                "operationType" : BooleanOperationType.SUBTRACTION });
            opBoolean(context, pid + "leSealU", {
                "targets" : movable, "tools" : leSeal,
                "operationType" : BooleanOperationType.UNION, "targetsAndToolsNeedGrouping" : true });
        }

        if (definition.addHorn)
        {
            const hPlane = evPlane(context, { "face" : definition.hornPlane });
            const hs     = along(hPlane.origin, pinAxis); 
            const hSign  = definition.hornPlusThickness ? 1 : -1;
            const hT     = definition.hornThickness;
            const holeR  = definition.hornBore / 2;
            const beta   = definition.leBevel;

            const probeAft = hingePos + gap + definition.hornChord + 2 * wallDepth;
            const probe = csSectionSlab(context, id + "hornProbe", solidRef,
                                        hingePos - wallDepth, probeAft, hs - hT / 2, hs + hT / 2,
                                        cutN, pinAxis, center, big);
            const probeBox = evBox3d(context, {
                "topology" : probe,
                "cSys"     : coordSystem(center, cutN, thickDir),
                "tight"    : true
            });
            opDeleteBodies(context, id + "hornProbeDel", { "entities" : probe });
            // top or bottom
            const wSkin = hSign > 0 ? probeBox.maxCorner[2] : -probeBox.minCorner[2];

            const hornClr   = definition.hornClearance;
            const wSafe     = wSkin + hornClr;
            const tipMargin = max(1.5 * millimeter, definition.hornBore); // stock around the hole
            const rLug      = holeR + tipMargin;                          // tip lug radius
            const arm       = wSkin + definition.hornHeight;
            const armTop    = arm + rLug;      

            if (definition.hornHeight < holeR + hornClr)
            {
                throw regenError("Horn height is "
                    ~ toString(roundToPrecision(definition.hornHeight / millimeter, 1))
                    ~ " mm, so the " ~ toString(roundToPrecision(definition.hornBore / millimeter, 1))
                    ~ " mm hole would break through the skin it stands on instead of clearing it. Raise "
                    ~ "it to at least "
                    ~ toString(roundToPrecision((holeR + hornClr) / millimeter, 1))
                    ~ " mm (hole radius + horn clearance).", ["hornHeight"]);
            }

            // so on cs
            const uStart  = max(hingePos + gap, hingeAxisPos + wSkin * tan(beta) + hornClr);
            const uHole   = uStart + rLug;
            const uEnd    = uStart + definition.hornChord;
            const uLugAft = uHole + rLug;

            const holeCtr = center + cutN * uHole + thickDir * (hSign * arm);
            if (uEnd < uLugAft + tipMargin)
            {
                throw regenError("Horn root chord ("
                    ~ toString(roundToPrecision(definition.hornChord / millimeter, 1)) ~ " mm) is shorter "
                    ~ "than the tip lug it has to taper down to -- there is nothing left to slope. Raise "
                    ~ "it to at least "
                    ~ toString(roundToPrecision((uLugAft + tipMargin - uStart) / millimeter, 1))
                    ~ " mm, or shrink the hole.", ["hornChord"]);
            }

            const gFlip = definition.hornGusset && definition.hornGussetFlip;
            const gW    = definition.hornGusset ? armTop - wSkin : 0 * millimeter;
            const vLo   = hs - hT / 2 - (gFlip ? 0 * millimeter : gW);
            const vHi   = hs + hT / 2 + (gFlip ? gW : 0 * millimeter);
            const ribPad = max(1.5 * millimeter, wallDepth);
            const ribLo  = min(vLo, hs - hT / 2 - ribPad);
            const ribHi  = hs + hT / 2 + ribPad;

            const rib = csSectionSlab(context, id + "hornRib", solidRef, uStart, uEnd, ribLo, ribHi,
                                      cutN, pinAxis, center, big);
            const horn = csOrientedBox(context, id + "hornBox", center, cutN, pinAxis, thickDir,
                                       uStart, uEnd, vLo, vHi,
                                       min(0 * millimeter, hSign * armTop),
                                       max(0 * millimeter, hSign * armTop));

            const aDir = hSign * thickDir;                        // thick * horn side
            const P    = center + cutN * uEnd + thickDir * (hSign * wSkin); // horn far corner
            const v    = holeCtr - P;                             // aft base corner -> hole centre
            const d    = norm(v);
            if (d <= rLug)
            {
                throw regenError("Horn is too small to carry its own hole: the aft base corner is only "
                    ~ toString(roundToPrecision(d / millimeter, 2)) ~ " mm from the hole centre, inside "
                    ~ "the " ~ toString(roundToPrecision(rLug / millimeter, 2)) ~ " mm lug the edge has "
                    ~ "to wrap around -- there is no triangle left, just a hole in a stub. Raise the "
                    ~ "horn height or the root chord, or shrink the hole.", ["hornHeight"]);
            }
            // holy math
            const e  = v / d; 
            const eP = -dot(e, aDir) * cutN + dot(e, cutN) * aDir; // 90° rotation with chord.thick plane
            const th = -asin(rLug / d); // angle to top
            const tv = e * cos(th) + eP * sin(th); // turning e
            var n = -dot(tv, aDir) * cutN + dot(tv, cutN) * aDir; // normal to tv
            if (dot(n, v) > 0 * millimeter) { n = -n; } 
            opBoolean(context, id + "hornTaper", {
                "targets" : horn,
                "tools"   : csHalfSpace(context, id + "hornTaperHS", P, n, pinAxis, big),
                "operationType" : BooleanOperationType.SUBTRACTION });

            if (definition.hornGusset)
            {
                const gDir     = gFlip ? 1 : -1;                              // span side the web flares to
                const rampBase = center + pinAxis * (hs + gDir * hT / 2);     // ramp base corner 
                const rampOut  = normalize(hSign * thickDir + gDir * pinAxis);
                opBoolean(context, id + "hornRamp", {
                    "targets" : horn,
                    "tools"   : csHalfSpace(context, id + "hornRampHS",
                                            rampBase + rampOut * (armTop / sqrt(2)), rampOut, cutN, big),
                    "operationType" : BooleanOperationType.SUBTRACTION });
            }

            opBoolean(context, id + "hornRibU", {
                "targets" : movable, "tools" : rib,
                "operationType" : BooleanOperationType.UNION,
                "targetsAndToolsNeedGrouping" : true });
            opBoolean(context, id + "hornU", {
                "targets" : movable, "tools" : horn,
                "operationType" : BooleanOperationType.UNION,
                "targetsAndToolsNeedGrouping" : true });

            //clear hole
            const hbore = csCylinder(context, id + "hbore", holeCtr, pinAxis,
                                     vLo - 1 * millimeter, vHi + 1 * millimeter, holeR);
            opBoolean(context, id + "hboreSub", {
                "targets" : movable, "tools" : hbore,
                "operationType" : BooleanOperationType.SUBTRACTION });
        }

        if (definition.addServoBay)
        {
            const svPlane = evPlane(context, { "face" : definition.servoPlane });
            const svs     = along(svPlane.origin, pinAxis) + definition.servoSpanOffset; // span station + sideways nudge
            const svSign  = definition.servoPlusThickness ? 1 : -1;
            const svClr   = definition.servoClearance;
            const svDepth = definition.servoDepth;
            const halfSp  = definition.servoScrewSpacing / 2;
            const screwR  = definition.servoScrewBore / 2;

            // case
            const uHalf = (definition.servoSpanwise ? definition.servoWidth : definition.servoLength) / 2 + svClr;
            const vHalf = (definition.servoSpanwise ? definition.servoLength : definition.servoWidth) / 2 + svClr;
            const uC    = hingeAxisPos - definition.servoOffset;

            // top sleeve
            const pad   = max(1.5 * millimeter, wallDepth);
            const uBoss = definition.servoSpanwise ? uHalf + pad : halfSp + screwR + pad;
            const vBoss = definition.servoSpanwise ? halfSp + screwR + pad : vHalf + pad;

            const earHalf = definition.servoSpanwise ? vHalf : uHalf;
            if (halfSp <= earHalf + screwR)
            {
                throw regenError("Screw spacing is "
                    ~ toString(roundToPrecision(definition.servoScrewSpacing / millimeter, 1))
                    ~ " mm, which drops the screw holes into the pocket mouth instead of the collar "
                    ~ "beside it -- the mounting ears straddle the case, so the spacing must exceed the "
                    ~ "case length (plus clearance and the hole itself), i.e. at least "
                    ~ toString(roundToPrecision((2 * (earHalf + screwR)) / millimeter, 1)) ~ " mm. "
                    ~ "Measure the real servo ear to ear.", ["servoScrewSpacing"]);
            }

            if (uC + uBoss > hingePos - wallDepth - 1 * millimeter)
            {
                throw regenError("Servo bay reaches back to "
                    ~ toString(roundToPrecision((uC + uBoss) / millimeter, 1)) ~ " mm along the chord, "
                    ~ "into the cove wall the knuckles anchor in (which starts at "
                    ~ toString(roundToPrecision((hingePos - wallDepth) / millimeter, 1))
                    ~ " mm) -- the collar would fill the hinge. Raise the forward offset to at least "
                    ~ toString(roundToPrecision((hingeAxisPos - (hingePos - wallDepth - 1 * millimeter) + uBoss) / millimeter, 1))
                    ~ " mm.", ["servoOffset"]);
            }

            if (svs - vBoss < pLo - 0.05 * millimeter || svs + vBoss > pHi + 0.05 * millimeter)
            {
                throw regenError("Servo bay footprint ("
                    ~ toString(roundToPrecision((svs - vBoss) / millimeter, 1)) ~ " .. "
                    ~ toString(roundToPrecision((svs + vBoss) / millimeter, 1)) ~ " mm along the hinge "
                    ~ "axis, collar included) runs past the end of the fixed surface ("
                    ~ toString(roundToPrecision(pLo / millimeter, 1)) ~ " .. "
                    ~ toString(roundToPrecision(pHi / millimeter, 1)) ~ " mm). Move the servo station "
                    ~ "in.", ["servoPlane"]);
            }

            const svProbe = csSectionSlab(context, id + "bayProbe", solidRef,
                                          uC - uHalf, uC + uHalf, svs - vHalf, svs + vHalf,
                                          cutN, pinAxis, center, big);
            const svBox = evBox3d(context, {
                "topology" : svProbe,
                "cSys"     : coordSystem(center, cutN, thickDir),
                "tight"    : true
            });
            opDeleteBodies(context, id + "bayProbeDel", { "entities" : svProbe });
            const wNear = svSign > 0 ? svBox.maxCorner[2] : -svBox.minCorner[2];
            const wFar  = svSign > 0 ? -svBox.minCorner[2] : svBox.maxCorner[2];

            if (svDepth > wNear + wFar - definition.skinThickness)
            {
                throw regenError("Servo pocket is "
                    ~ toString(roundToPrecision(svDepth / millimeter, 1)) ~ " mm deep, but the section "
                    ~ "at this station is only "
                    ~ toString(roundToPrecision((wNear + wFar) / millimeter, 1)) ~ " mm thick -- the "
                    ~ "pocket would break clean through the far skin. Move the bay FORWARD (larger "
                    ~ "offset) to a thicker station, or let more of the case stand proud.",
                    ["servoDepth"]);
            }

            const collarRaw = csSectionSlab(context, id + "bayCollar", solidRef,
                                            uC - uBoss, uC + uBoss, svs - vBoss, svs + vBoss,
                                            cutN, pinAxis, center, big);
            // Slice along svSign*thickDir, so the coordinate reads "distance from the mid-plane out
            // toward the servo's own skin" on either side, sign and all.
            const collar = csSliceSpan(context, id + "bayCollarW", collarRaw,
                                       wNear - definition.servoCollar, big,
                                       svSign * thickDir, cutN, center, big);
            opDeleteBodies(context, id + "bayCollarDel", { "entities" : collarRaw });
            opBoolean(context, id + "bayCollarU", {
                "targets" : fixed, "tools" : collar,
                "operationType" : BooleanOperationType.UNION,
                "targetsAndToolsNeedGrouping" : true });

            const pocket = csOrientedBox(context, id + "bayPocket", center, cutN, pinAxis, thickDir,
                                         uC - uHalf, uC + uHalf, svs - vHalf, svs + vHalf,
                                         svSign > 0 ? wNear - svDepth : -big,
                                         svSign > 0 ? big : svDepth - wNear);
            opBoolean(context, id + "bayPocketSub", {
                "targets" : fixed, "tools" : pocket,
                "operationType" : BooleanOperationType.SUBTRACTION });

            var screws = [];
            for (var j = 0; j < 2; j += 1)
            {
                const sgn = j == 0 ? -1 : 1;
                const sc  = center
                          + cutN    * (definition.servoSpanwise ? uC : uC + sgn * halfSp)
                          + pinAxis * (definition.servoSpanwise ? svs + sgn * halfSp : svs);
                screws = append(screws, csCylinder(context, id + ("bayScrew" ~ toString(j)),
                                                   sc, svSign * thickDir,
                                                   wNear - definition.servoCollar - 1 * millimeter,
                                                   wNear + 2 * millimeter, screwR));
            }
            opBoolean(context, id + "bayScrewSub", {
                "targets" : fixed, "tools" : qUnion(screws),
                "operationType" : BooleanOperationType.SUBTRACTION });
        }
        opDeleteBodies(context, id + "delSolidRef", { "entities" : solidRef });

        const datumP  = evPlane(context, { "face" : definition.knuckleDatum });
        const datumS  = along(datumP.origin, pinAxis);
        const pitch   = definition.knucklePitch;
        const R       = definition.knuckleRadius;
        const L        = definition.knuckleLength;
        const boreR   = definition.pinDiameter / 2 + definition.boreClearance;
        const clrR    = R + definition.fitClearance;

        // First/last pattern index whose knuckle fits inside [spanLo, spanHi].
        const kFirst = ceil((spanLo + L / 2 - datumS) / pitch);
        const kLast  = floor((spanHi - L / 2 - datumS) / pitch);
        var parity = false;

        for (var k = kFirst; k <= kLast; k += 1)
        {
            const sc = datumS + k * pitch;        // knuckle center along pinAxis
            const s0 = sc - L / 2;
            const s1 = sc + L / 2;

            if (sc < pLo || sc > pHi) { continue; }
            const host    = parity ? movable : fixed;
            const foreign = parity ? fixed   : movable;

            const kid = id + ("k" ~ toString(k - kFirst));

            const axisBase = center + cutN * hingeAxisPos; // + thickDir*0
            const barrel = csCylinder(context, kid + "bar", axisBase, pinAxis, s0, s1, R);

            if (definition.knuckleChamfer != KnuckleChamferEnd.PLUS)  // MINUS or BOTH
            {
                csAddTeardrop(context, kid + "tdLo", barrel, axisBase, pinAxis, s0, R, boreR, -1);
            }
            if (definition.knuckleChamfer != KnuckleChamferEnd.MINUS) // PLUS or BOTH
            {
                csAddTeardrop(context, kid + "tdHi", barrel, axisBase, pinAxis, s1, R, boreR, 1);
            }

            opBoolean(context, kid + "hostU", {
                "targets" : host, "tools" : barrel,
                "operationType" : BooleanOperationType.UNION,
                "targetsAndToolsNeedGrouping" : true });

            const clr = csCylinder(context, kid + "clr", axisBase, pinAxis, s0 - gap, s1 + gap, clrR);
            opBoolean(context, kid + "clrSub", {
                "targets" : foreign, "tools" : clr,
                "operationType" : BooleanOperationType.SUBTRACTION });
            parity = !parity;
        }

        const boreAxis = center + cutN * hingeAxisPos;
        var boreLo = spanLo - 5 * millimeter;
        var boreHi = spanHi + 5 * millimeter;
        if (definition.pinChannel)
        {
            if (definition.pinChannelOutboard) { boreHi = shellBox.maxCorner[0] + 5 * millimeter; }
            else                               { boreLo = shellBox.minCorner[0] - 5 * millimeter; }
        }
        const bid  = id + "bore";
        const bore = csCylinder(context, bid, boreAxis, pinAxis, boreLo, boreHi, boreR);
        opBoolean(context, bid + "sub", {
            "targets" : qUnion([fixed, movable]), "tools" : bore,
            "operationType" : BooleanOperationType.SUBTRACTION });
    });

// functions
function csSectionSlab(context is Context, id is Id, solid is Query,
                       cutLo is ValueWithUnits, cutHi is ValueWithUnits,
                       spanLo is ValueWithUnits, spanHi is ValueWithUnits,
                       cutN is Vector, pinAxis is Vector, center is Vector, big is ValueWithUnits) returns Query
{
    const inSpan = csSliceSpan(context, id + "sp", solid, spanLo, spanHi, pinAxis, cutN, center, big);
    const slab   = csSliceSpan(context, id + "ct", inSpan, cutLo, cutHi, cutN, pinAxis, center, big);
    // csSliceSpan dups its input, so `inSpan` is now an orphaned copy of the solid reference.
    // Delete it -- otherwise every csSectionSlab call leaks one leftover "cut of the solid" body
    // (two per feature: the cove wall and the LE wall).
    opDeleteBodies(context, id + "delInSpan", { "entities" : inSpan });
    return slab;
}

function csCylinder(context is Context, id is Id, axisBase is Vector, axisDir is Vector,
                    s0 is ValueWithUnits, s1 is ValueWithUnits, R is ValueWithUnits) returns Query
{
    fCylinder(context, id, {
        "bottomCenter" : axisBase + axisDir * s0,
        "topCenter"    : axisBase + axisDir * s1,
        "radius"       : R
    });
    return qCreatedBy(id, EntityType.BODY);
}

function csAddTeardrop(context is Context, id is Id, target is Query, axisBase is Vector,
                       axisDir is Vector, faceS is ValueWithUnits, R is ValueWithUnits,
                       tipR is ValueWithUnits, grow is number)
{
    const nStep = 4;
    const drop  = R - tipR;
    if (drop <= 0 * millimeter) { return; }
    const dz = drop / nStep;
    var discs = [];
    for (var j = 0; j < nStep; j += 1)
    {
        const rj = R - j * dz;                 // full radius at the face, shrinking outward -> ~45deg
        const dj = csCylinder(context, id + ("d" ~ toString(j)), axisBase, axisDir,
                              faceS + grow * j * dz, faceS + grow * (j + 1) * dz, rj);
        discs = append(discs, dj);
    }
    opBoolean(context, id + "U", {
        "targets" : target, "tools" : qUnion(discs),
        "operationType" : BooleanOperationType.UNION,
        "targetsAndToolsNeedGrouping" : true
    });
}

function csOrientedBox(context is Context, id is Id, center is Vector,
                       uDir is Vector, vDir is Vector, wDir is Vector,
                       uLo is ValueWithUnits, uHi is ValueWithUnits,
                       vLo is ValueWithUnits, vHi is ValueWithUnits,
                       wLo is ValueWithUnits, wHi is ValueWithUnits) returns Query
{   
    // make cuboid in world, using the needed distances from center
    fCuboid(context, id, {
        "corner1" : vector(uLo, vLo, wLo),
        "corner2" : vector(uHi, vHi, wHi)
    });
    // move it to center and rotates using new axes
    opTransform(context, id + "xf", {
        "bodies"    : qCreatedBy(id, EntityType.BODY),
        "transform" : toWorld(coordSystem(center, uDir, wDir))
    });
    return qCreatedBy(id, EntityType.BODY);
}

function csDup(context is Context, id is Id, body is Query) returns Query
{
    opPattern(context, id, {
        "entities" : body, "transforms" : [identityTransform()], "instanceNames" : ["1"]
    });
    return qCreatedBy(id, EntityType.BODY);
}

function csHalfSpace(context is Context, id is Id, planePoint is Vector, outDir is Vector,
                     nDir is Vector, big is ValueWithUnits) returns Query
{
    const cId = id + "c";
    fCuboid(context, cId, {
        "corner1" : vector(0 * meter, -big, -big),
        "corner2" : vector(big,        big,  big)
    });
    opTransform(context, id + "cXf", {
        "bodies"    : qCreatedBy(cId, EntityType.BODY),
        "transform" : toWorld(coordSystem(planePoint, outDir, nDir))
    });
    return qCreatedBy(cId, EntityType.BODY);
}
// both ways along span
function csSliceSpan(context is Context, id is Id, body is Query,
                     loSpan is ValueWithUnits, hiSpan is ValueWithUnits,
                     spanDir is Vector, nDir is Vector, center is Vector, big is ValueWithUnits) returns Query
{
    const sliced = csDup(context, id + "d", body);
    opBoolean(context, id + "lo", {
        "targets" : sliced,
        "tools"   : csHalfSpace(context, id + "loHS", center + spanDir * loSpan, -spanDir, nDir, big),
        "operationType" : BooleanOperationType.SUBTRACTION });
    opBoolean(context, id + "hi", {
        "targets" : sliced,
        "tools"   : csHalfSpace(context, id + "hiHS", center + spanDir * hiSpan, spanDir, nDir, big),
        "operationType" : BooleanOperationType.SUBTRACTION });
    return sliced;
}